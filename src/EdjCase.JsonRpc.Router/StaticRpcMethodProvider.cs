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
		private IRpcContextAccessor contextAccessor { get; }

		public StaticRpcMethodProvider(StaticRpcMethodDataAccessor dataAccessor,
			IRpcContextAccessor contextAccessor)
		{
			this.dataAccessor = dataAccessor;
			this.contextAccessor = contextAccessor;
		}

		public IReadOnlyList<MethodInfo> Get()
		{
			IRpcContext context = this.contextAccessor.Value!;
			StaticRpcMethodData? data = this.dataAccessor.Value;
			if(data == null)
			{
				throw new InvalidOperationException("No rpc method data is avaliable. It must be added to the request pipeline.");
			}
			if (context.Path == null)
			{
				return data.BaseMethods;
			}
			bool result = data.Methods.TryGetValue(context.Path, out List<MethodInfo>? m);

			return result ? m! : (IReadOnlyList<MethodInfo>)Array.Empty<MethodInfo>();
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
		public StaticRpcMethodData? Value { get; set;}
	}
}