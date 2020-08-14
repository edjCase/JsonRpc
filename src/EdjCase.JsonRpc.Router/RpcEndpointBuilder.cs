
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdjCase.JsonRpc.Router.Abstractions;

namespace EdjCase.JsonRpc.Router
{
	public class RpcEndpointBuilder
	{
		private List<IRpcMethodInfo> baseMethods { get; } = new List<IRpcMethodInfo>();
		private Dictionary<RpcPath, List<IRpcMethodInfo>> methods { get; } = new Dictionary<RpcPath, List<IRpcMethodInfo>>();

		public RpcEndpointBuilder AddMethod(MethodInfo methodInfo, RpcPath? path = null)
		{
			IRpcMethodInfo rpcMethodInfo = DefaultRpcMethodInfo.FromMethodInfo(methodInfo);
			this.Add(path, rpcMethodInfo);
			return this;
		}
		public RpcEndpointBuilder AddMethod(IRpcMethodInfo methodInfo, RpcPath? path = null)
		{
			this.Add(path, methodInfo);
			return this;
		}

		public RpcEndpointBuilder AddController<T>()
		{
			Type controllerType = typeof(T);
			return this.AddController(controllerType);
		}
		public RpcEndpointBuilder AddController(Type controllerType)
		{
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
			return this.AddControllerWithCustomPath(controllerType, routePath);
		}

		public RpcEndpointBuilder AddControllerWithCustomPath<T>(RpcPath? path = null)
		{
			Type controllerType = typeof(T);
			return this.AddControllerWithCustomPath(controllerType, path);
		}
		public RpcEndpointBuilder AddControllerWithCustomPath(Type type, RpcPath? path = null)
		{
			IEnumerable<MethodInfo> methods = RpcEndpointBuilder.Extract(type);
			foreach (MethodInfo methodInfo in methods)
			{
				this.AddMethod(methodInfo, path);
			}
			return this;
		}

		internal StaticRpcMethodData Resolve()
		{
			return new StaticRpcMethodData(this.baseMethods, this.methods);
		}

		private static IEnumerable<MethodInfo> Extract(Type controllerType)
		{
			return controllerType.Assembly.GetTypes()
				.Where(t => t == controllerType)
				.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
				.Where(m => m.DeclaringType != typeof(object) && m.DeclaringType != typeof(RpcController));
		}

		private void Add(RpcPath? path, IRpcMethodInfo methodInfo)
		{
			List<IRpcMethodInfo> methods;
			if (path == null)
			{
				methods = this.baseMethods;
			}
			else
			{
				if (!this.methods.TryGetValue(path, out List<IRpcMethodInfo>? m))
				{
					methods = this.methods[path] = new List<IRpcMethodInfo>();
				}
				else
				{
					methods = m!;
				}
			}
			methods.Add(methodInfo);
		}
	}
}