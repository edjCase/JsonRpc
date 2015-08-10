using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class RpcRouterConfiguration
	{
		internal List<RpcSection> Sections { get; } = new List<RpcSection>();
		public PathString RoutePrefix { get; set; }

		public void RegisterClassToRpcSection<T>(string sectionName = null)
		{
			Type type = typeof(T);
			RpcSection section = this.Sections.FirstOrDefault(s => string.Equals(s.Name, sectionName, StringComparison.OrdinalIgnoreCase));

			if (section == null)
			{
				section = new RpcSection(sectionName);
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
