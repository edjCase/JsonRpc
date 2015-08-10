using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public class RpcSection
	{
		public string Name { get; }
		public List<Type> Types { get; } = new List<Type>();

		public RpcSection(string name)
		{
			this.Name = name;
		}
	}
}
