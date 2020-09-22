using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using EdjCase.JsonRpc.Router.Swagger.Documentation.Extensions;
using EdjCase.JsonRpc.Router.Swagger.Documentation.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdjCase.JsonRpc.Router.Swagger.Documentation
{
    public class JsonRpcSwaggerProvider : ISwaggerProvider
    {
        private readonly ISchemaGenerator schemaGenerator;
        private readonly SwaggerConfiguration swagerOptions;
        private readonly IJsonRpcMetadataProvider jsonRpcMetadataProvider;
        private readonly IXmlDocumentationService xmlDocumentationService;
        private OpenApiDocument cacheDocument;
        private JsonNamingPolicy namePolicy;

        public JsonRpcSwaggerProvider(
            ISchemaGenerator schemaGenerator,
            IJsonRpcMetadataProvider jsonRpcMetadataProvider,
            IXmlDocumentationService xmlDocumentationService,
            IOptions<SwaggerConfiguration> swaggerOptions
        )
        {
            this.schemaGenerator = schemaGenerator;
            this.swagerOptions = swaggerOptions.Value;
            this.namePolicy = swaggerOptions.Value.NamingPolicy;
            this.jsonRpcMetadataProvider = jsonRpcMetadataProvider;
            this.xmlDocumentationService = xmlDocumentationService;
        }

        private List<RpcRouteMethodInfo> GetUniqueKeyMethodPairs(IEnumerable<RouteInfo> routeInfos)
        {
            var uniqueRouteMethodInfos = new List<RpcRouteMethodInfo>();
            
            foreach (var routeInfo in routeInfos)
            {
                var methodsGroups = routeInfo.MethodInfos
                    .GroupBy(x=>x.Name); //group by name for generate unique url similar method names
                
                foreach (var methodsGroup in methodsGroups)
                {
                    int counterMethod = 1;
                    foreach (var methodInfo in methodsGroup)
                    {
                        string methodName = this.namePolicy.ConvertName(methodInfo.Name);
                        string uniqueUrl = $"/{routeInfo.Path}#{methodName}";
                        
                        if (methodsGroup.Count() > 1)
                        {
                            uniqueUrl += $"#{counterMethod++}";
                        }
                        
                        uniqueRouteMethodInfos.Add(new RpcRouteMethodInfo()
                        {
                            UniqueUrl = uniqueUrl,
                            MethodInfo = methodInfo,
                            MethodName = methodName
                        });
                    }
                }
            }

            return uniqueRouteMethodInfos;
        }
        
        public OpenApiDocument GetSwagger(string documentName, string host = null, string basePath = null)
        {
            if (this.cacheDocument != null)
                return this.cacheDocument;
            
            var schemaRepository = new SchemaRepository();
            var groups = this.jsonRpcMetadataProvider.GetRpcMethodInfos();
            OpenApiPaths paths = this.GetOpenApiPaths(groups, schemaRepository);

            this.cacheDocument = new OpenApiDocument()
            {
                Info = new OpenApiInfo()
                {
                    Title = Assembly.GetEntryAssembly().GetName().Name,
                    Version = "v1"
                },
                Servers = this.swagerOptions.Endpoints.Select(x=> new OpenApiServer()
                {
                    Url = x
                }).ToList(),
                Components = new OpenApiComponents()
                {
                    Schemas = schemaRepository.Schemas
                },
                Paths = paths
            };

            return this.cacheDocument;
        }

        private OpenApiPaths GetOpenApiPaths(IEnumerable<RouteInfo> groups, SchemaRepository schemaRepository)
        {
            OpenApiPaths paths = new OpenApiPaths();
            
            var uniqueRouteMethodInfos = this.GetUniqueKeyMethodPairs(groups);
            
            foreach (var uniqueRouteMethodInfo in uniqueRouteMethodInfos)
            {
                var operation = this.GetOpenApiOperation(uniqueRouteMethodInfo.UniqueUrl,
                    uniqueRouteMethodInfo.MethodInfo, schemaRepository);

                var pathItem = new OpenApiPathItem()
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>()
                    {
                        [OperationType.Post] = operation
                    }
                };
                paths.Add(uniqueRouteMethodInfo.UniqueUrl, pathItem);
            }

            return paths;
        }

        private OpenApiOperation GetOpenApiOperation(string key, MethodInfo methodInfo,
            SchemaRepository schemaRepository)
        {
            var methodAnnotation = this.xmlDocumentationService.GetSummuryForMethod(methodInfo);
            var returnMethodType = methodInfo.GetReturnMethodType();
            var summaryForType = this.xmlDocumentationService.GetSummuryForType(methodInfo.DeclaringType);
            var methodGroupAnatation = !string.IsNullOrWhiteSpace(summaryForType)
                ? summaryForType
                : "Other";

            return new OpenApiOperation()
            {
                Tags = new List<OpenApiTag>()
                {
                    new OpenApiTag() {Name = methodGroupAnatation}
                },
                Summary = methodAnnotation,
                RequestBody = this.GetOpenApiRequestBody(key, methodInfo, schemaRepository),
                Responses = this.GetOpenApiResponses(returnMethodType, schemaRepository)
            };
        }

        private OpenApiResponses GetOpenApiResponses(Type returnMethodType, SchemaRepository schemaRepository)
        {
            var responseType = returnMethodType == typeof(void)
                ? typeof(JsonRpcResponse<object>)
                : typeof(JsonRpcResponse<>).MakeGenericType(returnMethodType);
            
            return new OpenApiResponses()
            {
                ["200"] = new OpenApiResponse()
                {
                    Content = new Dictionary<string, OpenApiMediaType>()
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = this.GeResposeSchema(responseType, schemaRepository)
                        }
                    }
                }
            };
        }

        private OpenApiRequestBody GetOpenApiRequestBody(string key, MethodInfo methodInfo,
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

        private OpenApiSchema GetBodyParamsSchema(string key, SchemaRepository schemaRepository, MethodInfo methodInfo)
        {
            key = key.Replace("/", "_").Replace("#", "|");
            
            var paramsObjectSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new SortedSet<string>(),
                AdditionalPropertiesAllowed = false
            };
            
            foreach (var p in methodInfo.GetParameters())
            {
                paramsObjectSchema.Properties.Add(this.namePolicy.ConvertName(p.Name), 
                    this.schemaGenerator.GenerateSchema(p.ParameterType, schemaRepository));    
            }
            
            paramsObjectSchema = schemaRepository.AddDefinition($"{key}", paramsObjectSchema);
            
            var requestSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new SortedSet<string>(),
                AdditionalPropertiesAllowed = false
            };
            
            requestSchema.Properties.Add("id", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
            requestSchema.Properties.Add("jsonrpc", this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
            requestSchema.Properties.Add("method",  this.schemaGenerator.GenerateSchema(typeof(string), schemaRepository));
            requestSchema.Properties.Add("params", paramsObjectSchema);
            
            requestSchema = schemaRepository.AddDefinition($"request_{key}", requestSchema);

            this.RewriteJrpcAttributesExamples(requestSchema, schemaRepository, this.namePolicy.ConvertName(methodInfo.Name));
            
            return requestSchema;
        }

        private OpenApiSchema GeResposeSchema(Type type, SchemaRepository schemaRepository)
        {
            var responseSchema = this.schemaGenerator.GenerateSchema(type, schemaRepository);
            this.RewriteJrpcAttributesExamples(responseSchema, schemaRepository);
            return responseSchema;
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