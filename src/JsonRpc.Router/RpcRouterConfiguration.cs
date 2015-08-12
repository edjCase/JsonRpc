using System;

namespace edjCase.JsonRpc.Router
{
	public class RpcRouterConfiguration
	{
		internal RpcRouteCollection Routes { get; }

		public string RoutePrefix
		{
			get { return this.Routes.RoutePrefix; }
			set { this.Routes.RoutePrefix = value; }
		}

		public RpcRouterConfiguration(string routePrefix = null)
		{
			this.Routes = new RpcRouteCollection(routePrefix);
		}

		public void RegisterClassToRpcRoute<T>(string routeName = null)
		{
			Type type = typeof(T);
			RpcRoute route = this.Routes.GetByName(routeName);

			if (route == null)
			{
				route = new RpcRoute(routeName);
				this.Routes.Add(route);
			}

			bool uniqueClass = route.AddClass<T>();
			if (uniqueClass)
			{
				return;
			}
			string alreadyRegisteredMessage = $"Type '{type.FullName}' has already been registered " +
											$"with the Rpc router under the route '{routeName}'";
			throw new ArgumentException(alreadyRegisteredMessage);
		}
	}
}
