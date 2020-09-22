using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EdjCase.JsonRpc.Router.Swagger.Documentation.Extensions
{
    public class SwaggerConfiguration
    {
        public string[] Endpoints { get; set; } = new string[] {"http://localhost:5000"};
        public JsonNamingPolicy NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;
    }
    
    public static class StartupExtensions
    {
        public static void AddJrpcSwaggerDocumentaion(this IServiceCollection services,
            JsonSerializerOptions jsonSerializerOptions, Action<SwaggerGenOptions> swaggerGenOptions = null)
        {
            swaggerGenOptions = swaggerGenOptions ?? StartupExtensions.GetDefaultSwaggerGenOptions();
            
            services.TryAddTransient<ISerializerDataContractResolver>(s =>
            {
                return new JsonSerializerDataContractResolver(jsonSerializerOptions);
            });
            //enable xml documentation generation in project options (release and debug)
            services.TryAddTransient<IXmlDocumentationService, XmlDocumentationService>();
            services.TryAddTransient<IJsonRpcMetadataProvider, CustomJsonRpcMetadataProvider>();
            services.AddSingleton<ISwaggerProvider, JsonRpcSwaggerProvider>();
            services.AddSwaggerGen(swaggerGenOptions);
        }

        public static void AddJrpcSwaggerUI(
            this IApplicationBuilder app, 
            Action<SwaggerOptions> swaggerOptions = null,
            Action<SwaggerUIOptions> swaggerUiOptions = null)
        {
            app.UseSwagger(swaggerOptions);
            app.UseSwaggerUI(swaggerUiOptions ?? StartupExtensions.GetDefaultSwaggerUIOptions());
        }
        
        private static Action<SwaggerGenOptions> GetDefaultSwaggerGenOptions()
        {
            return c => c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "My API V1", 
                Version = "v1"
            });
        }
        
        private static Action<SwaggerUIOptions> GetDefaultSwaggerUIOptions()
        {
            return c=> c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        }
    }
}