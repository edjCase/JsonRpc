using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EdjCase.JsonRpc.Router.Swagger.Extensions
{
	public static class StartupExtensions
	{
		public static IServiceCollection AddJsonRpcWithSwagger(this IServiceCollection services,
			Action<RpcServerConfiguration>? configureRpc = null,
			JsonSerializerOptions? jsonSerializerOptions = null,
			Action<SwaggerGenOptions>? configureSwaggerGen = null)
		{
			configureSwaggerGen = configureSwaggerGen ?? StartupExtensions.GetDefaultSwaggerGenOptions();

			jsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions();
			services.TryAddSingleton<ISerializerDataContractResolver>(s =>
			{
				return new JsonSerializerDataContractResolver(jsonSerializerOptions);
			});
			return services
				//enable xml documentation generation in project options (release and debug)
				.AddSingleton<IXmlDocumentationService, XmlDocumentationService>()
				.AddScoped<ISwaggerProvider, JsonRpcSwaggerProvider>()
				.AddSwaggerGen(configureSwaggerGen)
				.AddJsonRpc(configureRpc);
		}

		public static IApplicationBuilder UseJsonRpcWithSwagger(this IApplicationBuilder app,
			Action<RpcEndpointBuilder>? configureRpc = null,
			Action<SwaggerOptions>? configureSwagger = null,
			Action<SwaggerUIOptions>? configureSwaggerUI = null)
		{
			return app
				.UseSwagger(configureSwagger)
				.UseJsonRpc(configureRpc);
		}

		public static IApplicationBuilder UseJsonRpcWithSwaggerUI(this IApplicationBuilder app,
			Action<RpcEndpointBuilder>? configureRpc = null,
			Action<SwaggerOptions>? configureSwagger = null,
			Action<SwaggerUIOptions>? configureSwaggerUI = null)
		{
			return app
				.UseSwagger(configureSwagger)
				.UseSwaggerUI(configureSwaggerUI ?? StartupExtensions.GetDefaultSwaggerUIOptions())
				.UseJsonRpc(configureRpc);
		}





		private static Action<SwaggerGenOptions> GetDefaultSwaggerGenOptions()
		{
			return c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo {Title = "My API V1", Version = "v1"});

				var authSchemaKey = "basicAuth";
				c.AddSecurityDefinition(authSchemaKey,
					new OpenApiSecurityScheme()
					{
						In = ParameterLocation.Header,
						Type = SecuritySchemeType.Http,
						Scheme = "basic"
					});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement()
				{
					{
						new OpenApiSecurityScheme()
						{
							Reference = new OpenApiReference()
							{
								Id = authSchemaKey,
								Type = ReferenceType.SecurityScheme
							},
							In = ParameterLocation.Header,
							Name = "basic",
							Scheme = "basic"
						},
						new List<string>()
					}
				});
			};
		}

		private static Action<SwaggerUIOptions> GetDefaultSwaggerUIOptions()
		{
			return c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
		}
	}
}