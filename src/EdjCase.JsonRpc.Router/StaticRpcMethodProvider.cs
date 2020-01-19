using System.Collections.Generic;
using System.Reflection;

namespace EdjCase.JsonRpc.Router
{
	internal class StaticRpcMethodProvider : IRpcMethodProvider
	{
		private StaticRpcMethodDataAccessor dataAccessor { get; }
		private IRpcContextAccessor contextAccessor { get; }

		public StaticRpcMethodProvider(StaticRpcMethodDataAccessor dataAccessor,
			IRpcContextAccessor contextAccessor)
		{
			this.dataAccessor = dataAccessor;
			this.contextAccessor = contextAccessor;
		}

		public IReadOnlyList<MethodInfo> Get()
		{
			IRpcContext context = this.contextAccessor.Value;
			StaticRpcMethodData data = this.dataAccessor.Value;
			if (context.Path == null)
			{
				return data.BaseMethods;
			}
			bool result = data.Methods.TryGetValue(context.Path, out List<MethodInfo> m);

			return result ? m : null;
		}
	}

	internal class StaticRpcMethodData
	{
		public List<MethodInfo> BaseMethods { get; }
		public Dictionary<RpcPath, List<MethodInfo>> Methods { get; }

		public StaticRpcMethodData(List<MethodInfo> baseMethods, Dictionary<RpcPath, List<MethodInfo>> methods)
		{
			this.BaseMethods = baseMethods;
			this.Methods = methods;
		}
	}

	internal class StaticRpcMethodDataAccessor
	{
		public StaticRpcMethodData Value { get; set;}
	}
}