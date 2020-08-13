using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdjCase.JsonRpc.Router.Defaults;

namespace EdjCase.JsonRpc.Router
{
    public interface IJsonRpcMetadata
    {
        IEnumerable<RouteInfo> GetRpcMethodInfos();
    }

    public class RouteInfo
    {
        public RpcPath? Path { get; set; }
        public Dictionary<MethodInfo, RpcMethodInfo>? MethodInfos { get; set; }
    }
    
    internal class JsonRpcMetadata : IJsonRpcMetadata
    {
        public static StaticRpcMethodData? StaticRpcMethodData { get; internal set; }

        public IEnumerable<RouteInfo> GetRpcMethodInfos()
        {
            if(JsonRpcMetadata.StaticRpcMethodData == null)
                return Enumerable.Empty<RouteInfo>();

            var rpcMethods = JsonRpcMetadata.StaticRpcMethodData.Methods
                .Select(x =>
                {
                    return new RouteInfo()
                    {
                        Path = x.Key,
                        MethodInfos = x.Value.ToDictionary(k => k, DefaultRequestMatcher.BuildMethodInfo)
                    };
                }).ToList();
            
            rpcMethods.Add(new RouteInfo()
            {
                Path = null,
                MethodInfos = JsonRpcMetadata.StaticRpcMethodData.BaseMethods
                    .ToDictionary(k => k, DefaultRequestMatcher.BuildMethodInfo)
            });
            
            return rpcMethods;
        }
    }
}
