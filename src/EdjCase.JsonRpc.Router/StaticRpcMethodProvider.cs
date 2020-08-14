using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EdjCase.JsonRpc.Router
{
	internal class StaticRpcMethodProvider : IRpcMethodProvider
	{
		private StaticRpcMethodDataAccessor dataAccessor { get; }

		public StaticRpcMethodProvider(StaticRpcMethodDataAccessor dataAccessor)
		{
			this.dataAccessor = dataAccessor;
		}

		public IReadOnlyList<IRpcMethodInfo> GetByPath(RpcPath? path = null)
		{
			StaticRpcMethodData? data = this.dataAccessor.Value;
			if (data == null)
			{
				throw new InvalidOperationException("No rpc method data is avaliable. It must be added to the request pipeline.");
			}
			if (path == null)
			{
				return data.BaseMethods;
			}
			bool result = data.Methods.TryGetValue(path, out List<IRpcMethodInfo>? m);

			return result ? m! : (IReadOnlyList<IRpcMethodInfo>)Array.Empty<IRpcMethodInfo>();
		}
	}

	internal class StaticRpcMethodData
	{
		public List<IRpcMethodInfo> BaseMethods { get; }
		public Dictionary<RpcPath, List<IRpcMethodInfo>> Methods { get; }

		public StaticRpcMethodData(List<IRpcMethodInfo> baseMethods, Dictionary<RpcPath, List<IRpcMethodInfo>> methods)
		{
			this.BaseMethods = baseMethods;
			this.Methods = methods;
		}
	}

	internal class StaticRpcMethodDataAccessor
	{
		public StaticRpcMethodData? Value { get; set; }
	}
}