using System.Text.Json;

namespace EdjCase.JsonRpc.Router.Swagger.Models
{
	public class SwaggerConfiguration
	{
		public JsonNamingPolicy NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;
	}
}