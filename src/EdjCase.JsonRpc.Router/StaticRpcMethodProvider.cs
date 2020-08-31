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
				return data.BaseRoute;
			}
			bool result = data.PathRoutes.TryGetValue(path, out IReadOnlyList<IRpcMethodInfo>? m);

			return result ? m! : (IReadOnlyList<IRpcMethodInfo>)Array.Empty<IRpcMethodInfo>();
		}

		public IRpcRouteMetaData Get()
		{
			return this.dataAccessor.Value;
		}
	}

	internal class StaticRpcMethodData : IRpcRouteMetaData
	{
		public IReadOnlyList<IRpcMethodInfo> BaseRoute { get; }
		public IReadOnlyDictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>> PathRoutes { get; }

		public StaticRpcMethodData(IReadOnlyList<IRpcMethodInfo> baseMethods, IReadOnlyDictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>> methods)
		{
			this.BaseRoute = baseMethods;
			this.PathRoutes = methods;
		}
	}

	internal class StaticRpcMethodDataAccessor
	{
		public StaticRpcMethodData? Value { get; set; }
	}
}