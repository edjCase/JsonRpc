using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
#if !NETSTANDARD1_3
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
#endif

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// A url route and its corresponding registered classes containing Rpc methods
	/// </summary>
	public class RpcRoute
	{
		/// <summary>
		/// Name of the route
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Criteria for the route to be a match to an rpc request
		/// </summary>
		public IReadOnlyList<RouteCriteria> RouteCriteria { get; }

		/// <param name="routeCriteria">Criteria for the route to be matched to an rpc request</param>
		/// <param name="name">(Optional) Name for the route</param>
		/// <param name="group">(Optional) Group name for the route</param>
		public RpcRoute(List<RouteCriteria> routeCriteria, string name = null)
		{
			if (routeCriteria == null || !routeCriteria.Any())
			{
				throw new ArgumentException("At least one route criterion is required.");
			}
			this.Name = name;
			this.RouteCriteria = routeCriteria;
		}
	}



	/// <summary>
	/// TODO
	/// </summary>
	public class RpcRouteProvider : IRpcRouteProvider
	{
#if !NETSTANDARD1_3
		public bool AutoDetectControllers { get; set; } = true;
#endif
		/// <summary>
		/// List of the Rpc routes
		/// </summary>
		private List<RpcRoute> routeList { get; } = new List<RpcRoute>();

#if !NETSTANDARD1_3
		/// <summary>
		/// Adds all types that inherit <see cref="RpcController"/> to the route collection.
		/// Controllers defaults to the controller name for the route unless configured otherwise
		/// </summary>
		private List<RpcRoute> GetControllerRoutes()
		{
			Type rpcControllerType = typeof(RpcController);
			DependencyContext depedencyContext = DependencyContext.Default;
			IEnumerable<TypeInfo> controllerTypes = depedencyContext.RuntimeLibraries
				.SelectMany(l => l.GetDefaultAssemblyNames(depedencyContext))
				.Select(Assembly.Load)
				.SelectMany(a => a.DefinedTypes)
				.Where(t => !t.IsAbstract && t.IsSubclassOf(rpcControllerType));

			List<RpcRoute> controllerRoutes = new List<RpcRoute>();
			foreach (TypeInfo controllerType in controllerTypes)
			{
				var attribute = controllerType.GetCustomAttribute<RpcRouteAttribute>(true);
				string routeName;
				if (attribute == null || string.IsNullOrWhiteSpace(attribute.RouteName))
				{
					if (controllerType.Name.EndsWith("Controller"))
					{
						routeName = controllerType.Name.Substring(0, controllerType.Name.IndexOf("Controller"));
					}
					else
					{
						routeName = controllerType.Name;
					}
				}
				else
				{
					routeName = attribute.RouteName;
				}

				var routeCriteria = new List<RouteCriteria>
					{
						new RouteCriteria(controllerType.AsType())
					};
				controllerRoutes.Add(new RpcRoute(routeCriteria, routeName));
			}
			return controllerRoutes;
		}
#endif

		public List<RpcRoute> GetRoutes()
		{
			List<RpcRoute> routes = this.routeList.ToList();
#if !NETSTANDARD1_3
			if (this.AutoDetectControllers)
			{
				List<RpcRoute> controllerRoutes = this.GetControllerRoutes();
				routes.AddRange(controllerRoutes);
			}
#endif
			return routes;
		}


		public void RegisterRoute(RouteCriteria routeCriteria, string name = null)
		{
			if (routeCriteria == null)
			{
				throw new ArgumentException("At least one route criterion is required.");
			}
			this.routeList.Add(new RpcRoute(new List<RouteCriteria> { routeCriteria }, name));
		}

		public void RegisterRoute(IEnumerable<RouteCriteria> routeCriteria, string name = null)
		{
			List<RouteCriteria> routeCriteriaList = routeCriteria.ToList();
			if (routeCriteria == null || !routeCriteria.Any())
			{
				throw new ArgumentException("At least one route criterion is required.");
			}
			this.routeList.Add(new RpcRoute(routeCriteriaList, name));
		}
	}
}
