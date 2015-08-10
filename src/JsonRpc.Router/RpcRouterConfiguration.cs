using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class RpcRouterConfiguration
	{
		internal RpcRouteCollection Sections { get; } = new RpcRouteCollection();
		public PathString RoutePrefix { get; set; }

		public void RegisterClassToRpcSection<T>(string sectionName = null)
		{
			Type type = typeof(T);
			RpcRoute section = this.Sections.GetByName(sectionName);

			if (section == null)
			{
				section = new RpcRoute(sectionName);
				this.Sections.Add(section);
			}
			else if (section.Types.Any(t => t == type))
			{
				throw new ArgumentException($"Type '{type.FullName}' has already been registered with the Rpc router under the section '{sectionName}'");
			}

			section.Types.Add(type);
		}
	}
}
