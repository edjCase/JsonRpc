#if !NETSTANDARD1_3
using System.Collections.Generic;
using System.Linq;
using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Reflection;
using EdjCase.JsonRpc.Router.Criteria;

namespace EdjCase.JsonRpc.Router.RouteProviders
{
	/// <summary>
	/// Default route provider to give the router the configured routes to use
	/// </summary>
	public class RpcAutoRouteProvider : IRpcRouteProvider
	{
		public RpcAutoRoutingOptions Options { get; }

		public RpcAutoRouteProvider(RpcAutoRoutingOptions options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public RpcPath BaseRequestPath => this.Options.BaseRequestPath;


		private Dictionary<RpcPath, List<IRpcMethodProvider>> routeCache { get; set; }
		

		private Dictionary<RpcPath, List<IRpcMethodProvider>> GetAllRoutes()
		{
			if (this.routeCache == null)
			{
				//TODO will entry assembly be good enough
				List<TypeInfo> controllerTypes = Assembly.GetEntryAssembly().DefinedTypes
					.Where(t => !t.IsAbstract && t.IsSubclassOf(this.Options.BaseControllerType))
					.ToList();

				var controllerRoutes = new Dictionary<RpcPath, List<IRpcMethodProvider>>();
				foreach (TypeInfo controllerType in controllerTypes)
				{
					var attribute = controllerType.GetCustomAttribute<RpcRouteAttribute>(true);
					string routePathString;
					if (attribute == null || attribute.RouteName == null)
					{
						if (controllerType.Name.EndsWith("Controller"))
						{
							routePathString = controllerType.Name.Substring(0, controllerType.Name.IndexOf("Controller"));
						}
						else
						{
							routePathString = controllerType.Name;
						}
					}
					else
					{
						routePathString = attribute.RouteName;
					}
					RpcPath routePath = RpcPath.Parse(routePathString);
					if (!controllerRoutes.TryGetValue(routePath, out List<IRpcMethodProvider> methodProviders))
					{
						methodProviders = new List<IRpcMethodProvider>();
						controllerRoutes[routePath] = methodProviders;
					}
					methodProviders.Add(new ControllerPublicMethodProvider(controllerType.AsType()));
				}
				this.routeCache = controllerRoutes;
			}
			return this.routeCache;
		}

		/// <summary>
		/// Gets all the routes from all the controllers derived from the 
		/// configured base controller type
		/// </summary>
		/// <returns>All the available routes</returns>
		public HashSet<RpcPath> GetRoutes()
		{
			Dictionary<RpcPath, List<IRpcMethodProvider>> routes = this.GetAllRoutes();
			return new HashSet<RpcPath>(routes.Keys);
		}

		/// <summary>
		/// Gets all the method providers for the specified path
		/// </summary>
		/// <param name="path">Path to the methods</param>
		/// <returns>All method providers for the specified path</returns>
		public List<IRpcMethodProvider> GetMethodsByPath(RpcPath path)
		{
			Dictionary<RpcPath, List<IRpcMethodProvider>> routes = this.GetAllRoutes();
			if (!routes.TryGetValue(path, out List<IRpcMethodProvider> methods))
			{
				return new List<IRpcMethodProvider>();
			}
			return methods;
		}
	}
}
#endif
