using System.Reflection;

namespace EdjCase.JsonRpc.Router.Swagger.Documentation.Models
{
    public class RouteInfo
    {
        public string Path { get; set; }
        public MethodInfo[] MethodInfos { get; set; }
    }

    public class RpcRouteMethodInfo
    {
        public string UniqueUrl { get; set; }
        public string MethodName { get; set; }
        public MethodInfo MethodInfo { get; set; }
    }
}