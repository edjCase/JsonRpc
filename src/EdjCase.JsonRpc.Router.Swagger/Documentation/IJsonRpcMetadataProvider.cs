using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdjCase.JsonRpc.Router.Swagger.Documentation.Models;

namespace EdjCase.JsonRpc.Router.Swagger.Documentation
{
    public interface IJsonRpcMetadataProvider
    {
        IEnumerable<RouteInfo> GetRpcMethodInfos();
    }
    
    public class CustomJsonRpcMetadataProvider : IJsonRpcMetadataProvider
    {
        private static IEnumerable<RouteInfo> cache;
        
        /// <summary>
        /// TODO USE Methodata provider from Edjcase
        /// </summary>
        public IEnumerable<RouteInfo> GetRpcMethodInfos()
        {
            if (CustomJsonRpcMetadataProvider.cache == null)
            {
                Type baseControllerType = typeof(RpcController);
                IEnumerable<Type> controllers = Assembly
                    .GetEntryAssembly()
                    .GetReferencedAssemblies()
                    .Select(Assembly.Load)
                    .SelectMany(x => x.DefinedTypes)
                    .Concat(Assembly.GetEntryAssembly().DefinedTypes)
                    .Where(t => !t.IsAbstract && (t == baseControllerType || t.IsSubclassOf(baseControllerType)));

                CustomJsonRpcMetadataProvider.cache = controllers.Select(x =>
                {
                    var path = x.GetCustomAttribute<RpcRouteAttribute>()?.RouteName ?? x.Name;
                    return new RouteInfo()
                    {
                        Path = path,
                        MethodInfos = x
                            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => m.DeclaringType != typeof(object))
                            .Where(x => x.Name != "Ok" && x.Name != "Error")
                            .ToArray()
                    };
                });
            }
            return CustomJsonRpcMetadataProvider.cache;
        }
    }
}