using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Swagger.Extensions;
using EdjCase.JsonRpc.Router.Swagger.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdjCase.JsonRpc.Router.Swagger
{
	public class JsonRpcSwaggerProvider : ISwaggerProvider
	{
		private readonly ISchemaGenerator schemaGenerator;
		private readonly IRpcMethodProvider methodProvider;
		private readonly IXmlDocumentationService xmlDocumentationService;
		private readonly IOptions<SwaggerGenOptions> swaggerGenOptionsAccessor;
		private OpenApiDocument? cacheDocument;
		private JsonNamingPolicy namePolicy;

		public JsonRpcSwaggerProvider(
			ISchemaGenerator schemaGenerator,
			IRpcMethodProvider methodProvider,
			IXmlDocumentationService xmlDocumentationService,
			IOptions<SwaggerConfiguration> swaggerOptions,
			IOptions<SwaggerGenOptions> swaggerGenOptionsAccessor
		)
		{
			this.schemaGenerator = schemaGenerator;
			this.namePolicy = swaggerOptions.Value.NamingPolicy;
			this.methodProvider = methodProvider;
			this.xmlDocumentationService = xmlDocumentationService;
			this.swaggerGenOptionsAccessor = swaggerGenOptionsAccessor;
		}

		private List<UniqueMethod> GetUniqueKeyMethodPairs(RpcRouteMetaData metaData)
		{
			List<UniqueMethod> methodList = this.Convert(metaData.BaseRoute, path: null).ToList();

			foreach ((RpcPath path, IReadOnlyList<IRpcMethodInfo> pathRoutes) in metaData.PathRoutes)
			{
				methodList.AddRange(this.Convert(pathRoutes, path));
			}

			return methodList;
		}

		private IEnumerable<UniqueMethod> Convert(IEnumerable<IRpcMethodInfo> routeInfo, RpcPath? path)
		{
			//group by name for generate unique url similar method names
			foreach (IGrouping<string, IRpcMethodInfo> methodsGroup in routeInfo.GroupBy(x => x.Name))
			{
				int? methodCounter = methodsGroup.Count() > 1 ? 1 : (int?)null;
				foreach (IRpcMethodInfo methodInfo in methodsGroup)
				{
					string methodName = this.namePolicy.ConvertName(methodInfo.Name);
					string uniqueUrl = $"/{path}#{methodName}";

					if (methodCounter != null)
					{
						uniqueUrl += $"#{methodCounter++}";
					}

					yield return new UniqueMethod(uniqueUrl, methodInfo);
				}
			}
		}

		public OpenApiDocument GetSwagger(string documentName, string? host = null, string? basePath = null)
		{
			if (this.cacheDocument != null)
			{
				return this.cacheDocument;
			}

			var schemaRepository = new SchemaRepository();
			RpcRouteMetaData metaData = this.methodProvider.Get();
			OpenApiPaths paths = this.GetOpenApiPaths(metaData, schemaRepository);

			this.cacheDocument = new OpenApiDocument()
			{
				SecurityRequirements = this.swaggerGenOptionsAccessor.Value.SwaggerGeneratorOptions.SecurityRequirements,
				Info = new OpenApiInfo()
				{
					Title = Assembly.GetEntryAssembly().GetName().Name,
					Version = "v1"
				},
				Servers = this.swaggerGenOptionsAccessor.Value.SwaggerGeneratorOptions.Servers,
				Components = new OpenApiComponents()
				{
					Schemas = schemaRepository.Schemas,
					SecuritySchemes = this.swaggerGenOptionsAccessor.Value.SwaggerGeneratorOptions.SecuritySchemes
				},
				Paths = paths
			};

			return this.cacheDocument;
		}

		private OpenApiPaths GetOpenApiPaths(RpcRouteMetaData metaData, SchemaRepository schemaRepository)
		{
			OpenApiPaths paths = new OpenApiPaths();

			List<UniqueMethod> uniqueMethods = this.GetUniqueKeyMethodPairs(metaData);

			foreach (UniqueMethod method in uniqueMethods)
			{
				string operationKey = method.UniqueUrl.Replace("/", "_").Replace("#", "|");
				OpenApiOperation operation = this.GetOpenApiOperation(operationKey, method.Info, schemaRepository);

				var pathItem = new OpenApiPathItem()
				{
					Operations = new Dictionary<OperationType, OpenApiOperation>()
					{
						[OperationType.Post] = operation
					}
				};
				paths.Add(method.UniqueUrl, pathItem);
			}

			return paths;
		}

		private OpenApiOperation GetOpenApiOperation(string key, IRpcMethodInfo methodInfo, SchemaRepository schemaRepository)
		{
			string methodAnnotation = this.xmlDocumentationService.GetSummaryForMethod(methodInfo);
			Type trueReturnType = this.GetReturnType(methodInfo.RawReturnType);

			return new OpenApiOperation()
			{
				Tags = new List<OpenApiTag>(),
				Summary = methodAnnotation,
				RequestBody = this.GetOpenApiRequestBody(key, methodInfo, schemaRepository),
				Responses = this.GetOpenApiResponses(key, trueReturnType, schemaRepository)
			};
		}

		private Type GetReturnType(Type returnType)
		{
			if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				//Return the `Task` return type
				return returnType.GenericTypeArguments.First();
			}
			if (returnType == typeof(Task))
			{
				//Task with no return type
				return typeof(void);
			}
			return returnType;
		}

		private OpenApiResponses GetOpenApiResponses(string key, Type returnMethodType, SchemaRepository schemaRepository)
		{
			return new OpenApiResponses()
			{
				["200"] = new OpenApiResponse()
				{
					Content = new Dictionary<string, OpenApiMediaType>()
					{
						["application/json"] = new OpenApiMediaType
						{
							Schema = this.GeResposeSchema(key, returnMethodType, schemaRepository)
						}
					}
				}
			};
		}

		private OpenApiRequestBody GetOpenApiRequestBody(string key, IRpcMethodInfo methodInfo,
			SchemaRepository schemaRepository)
		{
			return new OpenApiRequestBody()
			{
				Content = new Dictionary<string, OpenApiMediaType>()
				{
					["application/json"] = new OpenApiMediaType()
					{
						Schema = this.GetBodyParamsSchema(key, schemaRepository, methodInfo)
					}
				}
			};
		}

		private OpenApiSchema GetBodyParamsSchema(string key, SchemaRepository schemaRepository, IRpcMethodInfo methodInfo)
		{
			OpenApiSchema paramsObjectSchema = this.GetOpenApiEmptyObject();

			foreach (IRpcParameterInfo parameterInfo in methodInfo.Parameters)
			{
				string name = this.namePolicy.ConvertName(parameterInfo.Name);
				OpenApiSchema schema = this.schemaGenerator.GenerateSchema(parameterInfo.RawType, schemaRepository);
				paramsObjectSchema.Properties.Add(name, schema);
			}

			paramsObjectSchema = schemaRepository.AddDefinition($"{key}", paramsObjectSchema);

			var requestSchema = this.GetOpenApiEmptyObject();

			requestSchema.Properties.Add("id", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
			requestSchema.Properties.Add("jsonrpc", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
			requestSchema.Properties.Add("method", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
			requestSchema.Properties.Add("params", paramsObjectSchema);

			requestSchema = schemaRepository.AddDefinition($"request_{key}", requestSchema);

			this.RewriteJrpcAttributesExamples(requestSchema, schemaRepository, this.namePolicy.ConvertName(methodInfo.Name));

			return requestSchema;
		}

		private OpenApiSchema GeResposeSchema(string key, Type returnMethodType, SchemaRepository schemaRepository)
		{
			var resultSchema = this.schemaGenerator.GenerateSchema(returnMethodType, schemaRepository);

			var responseSchema = this.GetOpenApiEmptyObject();
			responseSchema.Properties.Add("id", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
			responseSchema.Properties.Add("jsonrpc", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
			responseSchema.Properties.Add("result", resultSchema);

			responseSchema = schemaRepository.AddDefinition($"response_{key}", responseSchema);
			this.RewriteJrpcAttributesExamples(responseSchema, schemaRepository);
			return responseSchema;
		}

		private OpenApiSchema GetOpenApiEmptyObject()
		{
			return new OpenApiSchema
			{
				Type = "object",
				Properties = new Dictionary<string, OpenApiSchema>(),
				Required = new SortedSet<string>(),
				AdditionalPropertiesAllowed = false
			};
		}

		private void RewriteJrpcAttributesExamples(OpenApiSchema schema, SchemaRepository schemaRepository, string method = "method_name")
		{
			var jrpcAttributesExample =
				new OpenApiObject()
				{
					{"id", new OpenApiString(Guid.NewGuid().ToString())},
					{"jsonrpc", new OpenApiString("2.0")},
					{"method", new OpenApiString(method)},
				};

			foreach (var prop in schemaRepository.Schemas[schema.Reference.Id].Properties)
			{
				if (jrpcAttributesExample.ContainsKey(prop.Key))
				{
					prop.Value.Example = jrpcAttributesExample[prop.Key];
				}
			}
		}
	}
}