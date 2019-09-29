using System.Collections.Generic;
using System.Reflection;

namespace EdjCase.JsonRpc.Router
{
	internal class StaticRpcMethodProvider : IRpcMethodProvider
	{
		private List<MethodInfo> baseMethods { get; }
		private Dictionary<RpcPath, List<MethodInfo>> methods { get; }

		public StaticRpcMethodProvider(List<MethodInfo> baseMethods, Dictionary<RpcPath, List<MethodInfo>> methods)
		{
			this.baseMethods = baseMethods;
			this.methods = methods;
		}

		public bool TryGetByPath(RpcPath path, out IReadOnlyList<MethodInfo> methods)
		{
			if (path == null)
			{
				methods = this.baseMethods;
				return true;
			}
			bool result = this.methods.TryGetValue(path, out List<MethodInfo> m);

			if (result)
			{
				methods = m;
				return true;
			}
			methods = null;
			return false;
		}
	}
}