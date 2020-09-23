using System.Text.Json;

namespace EdjCase.JsonRpc.Router.Swagger.Models
{
	public class SwaggerConfiguration
	{
		public string[] Endpoints { get; set; } = new string[0];
		public JsonNamingPolicy NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;
	}
}