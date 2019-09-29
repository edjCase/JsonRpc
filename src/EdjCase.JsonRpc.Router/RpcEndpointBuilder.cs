
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EdjCase.JsonRpc.Router
{
	public class RpcEndpointBuilder
	{
		private List<MethodInfo> baseMethods { get; } = new List<MethodInfo>();
		private Dictionary<RpcPath, List<MethodInfo>> methods { get; } = new Dictionary<RpcPath, List<MethodInfo>>();

		public RpcEndpointBuilder AddMethod(RpcPath path, MethodInfo methodInfo)
		{
			this.Add(path, methodInfo);
			return this;
		}

		public RpcEndpointBuilder AddControllerWithDefaultPath<T>()
		{
			Type controllerType = typeof(T);
			var attribute = controllerType.GetCustomAttribute<RpcRouteAttribute>(true);
			ReadOnlySpan<char> routePathString;
			if (attribute == null || attribute.RouteName == null)
			{
				if (controllerType.Name.EndsWith("Controller"))
				{
					routePathString = controllerType.Name.AsSpan(0, controllerType.Name.IndexOf("Controller"));
				}
				else
				{
					routePathString = controllerType.Name.AsSpan();
				}
			}
			else
			{
				routePathString = attribute.RouteName.AsSpan();
			}
			RpcPath routePath = RpcPath.Parse(routePathString);
			return this.AddController<T>(routePath);
		}

		public RpcEndpointBuilder AddController<T>(RpcPath path = null)
		{
			IEnumerable<MethodInfo> methods = this.Extract<T>();
			foreach (MethodInfo method in methods)
			{
				this.Add(path, method);
			}
			return this;
		}

		public IRpcMethodProvider Resolve()
		{
			return new StaticRpcMethodProvider(this.baseMethods, this.methods);
		}

		private IEnumerable<MethodInfo> Extract<T>()
		{
			Type baseControllerType = typeof(T);
			return Assembly.GetAssembly(typeof(T)).GetTypes()
				.Where(t => !t.IsAbstract && (t == baseControllerType || t.IsSubclassOf(baseControllerType)))
				.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
				.Where(m => m.DeclaringType != typeof(object));
		}

		private void Add(RpcPath path, MethodInfo methodInfo)
		{
            List<MethodInfo> methods;
            if (path == null)
            {
                methods = this.baseMethods;
            }
            else
            {
                if (!this.methods.TryGetValue(path, out methods))
                {
                    methods = this.methods[path] = new List<MethodInfo>();
                }
            }
			methods.Add(methodInfo);
		}
	}
}