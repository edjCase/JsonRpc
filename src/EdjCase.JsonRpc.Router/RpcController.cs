using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public abstract class RpcController
	{
	}


#if !NETSTANDARD1_3
	/// <summary>
	/// Attribute to decorate a derived <see cref="RpcController"/> class
	/// </summary>
	public class RpcRouteAttribute : Attribute
	{
		/// <summary>
		/// Name of the route to be used in the router. If unspecified, will use controller name.
		/// </summary>
		public string RouteName { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="routeName">(Optional) Name of the route to be used in the router. If unspecified, will use controller name.</param>
		/// <param name="routeGroup">(Optional) Name of the group the route is in to allow route filtering per request.</param>
		public RpcRouteAttribute(string routeName = null)
		{
			this.RouteName = routeName?.Trim();
		}
	}
#endif
}
