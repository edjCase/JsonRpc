using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class RpcRouterConfiguration
	{
		internal Dictionary<string, List<Type>> Sections { get; set; } = new Dictionary<string, List<Type>>();
		public PathString RoutePrefix { get; set; }

		public void RegisterClassToRpcSection<T>(string sectionName = null)
		{
			Type type = typeof(T);
			if (this.Sections.ContainsKey(sectionName) && this.Sections[sectionName].Any(t => t == type))
			{
				throw new ArgumentException($"Type '{type.FullName}' has already been registered with the Rpc router under the section '{sectionName}'");
			}
			List<Type> typeList;
			if (!this.Sections.TryGetValue(sectionName, out typeList))
			{
				typeList = new List<Type>();
				this.Sections[sectionName] = typeList;
			}
			typeList.Add(type);
		}
	}
}
