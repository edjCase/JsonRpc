using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public abstract class RpcController
	{
	}

	public class RpcRouteAttribute : Attribute
	{
		public string RouteName { get; }
		public RpcRouteAttribute(string routeName)
		{
			routeName = routeName?.Trim();
			this.RouteName = routeName;
		}
	}
}
