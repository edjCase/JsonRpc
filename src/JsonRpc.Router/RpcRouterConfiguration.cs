using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class RpcRouterConfiguration
	{
		internal RpcRouteCollection Routes { get; } = new RpcRouteCollection();
		public PathString RoutePrefix { get; set; }

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
			if (!uniqueClass)
			{
				return;
			}
			string alreadyRegisteredMessage = $"Type '{type.FullName}' has already been registered " +
											$"with the Rpc router under the route '{routeName}'";
			throw new ArgumentException(alreadyRegisteredMessage);
		}
	}
}
