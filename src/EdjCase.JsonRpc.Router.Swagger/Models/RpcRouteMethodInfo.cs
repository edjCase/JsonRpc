using System.Reflection;

namespace EdjCase.JsonRpc.Router.Swagger.Models
{
    public class RpcRouteMethodInfo
    {
        public string UniqueUrl { get; set; }
        public string MethodName { get; set; }
        public MethodInfo MethodInfo { get; set; }
    }
}