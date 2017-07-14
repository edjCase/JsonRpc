using EdjCase.JsonRpc.Router.Criteria;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	/// <summary>
	/// Interface to provide the middleware with available routes and their criteria
	/// </summary>
	public interface IRpcRouteProvider
	{
		RpcPath BaseRequestPath { get; }
		HashSet<RpcPath> GetRoutes();
		List<IRpcMethodProvider> GetMethodsByPath(RpcPath path);
	}

#if !NETSTANDARD1_3
	public class RpcAutoRoutingOptions
	{
		/// <summary>
		/// Sets the required base path for the request url to match against
		/// </summary>
		public RpcPath BaseRequestPath { get; set; }

		public Type BaseControllerType { get; set; } = typeof(RpcController);
	}
#endif

	public class RpcManualRoutingOptions
	{
		/// <summary>
		/// Sets the required base path for the request url to match against
		/// </summary>
		public string BaseRequestPath { get; set; }

		public Dictionary<RpcPath, List<IRpcMethodProvider>> Routes { get; set; } = new Dictionary<RpcPath, List<IRpcMethodProvider>>();


		public void RegisterMethods(RpcPath path, IRpcMethodProvider methodProvider)
		{
			if(path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}
			if (!this.Routes.TryGetValue(path, out List<IRpcMethodProvider> methodProviders))
			{
				methodProviders = new List<IRpcMethodProvider>();
				this.Routes[path] = methodProviders;
			}
			methodProviders.Add(methodProvider);
		}

		public void RegisterController<T>(RpcPath path = default(RpcPath))
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}
			this.RegisterMethods(path, new ControllerTypeMethodProvider(typeof(T)));
		}
	}

	public interface IRpcMethodProvider
	{
		List<MethodInfo> GetRouteMethods();
	}
}
