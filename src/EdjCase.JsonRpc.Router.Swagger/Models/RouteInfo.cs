using System.Reflection;

namespace EdjCase.JsonRpc.Router.Swagger.Models
{
    public class RouteInfo
    {
        public string Path { get; set; }
        public MethodInfo[] MethodInfos { get; set; }
    }
}