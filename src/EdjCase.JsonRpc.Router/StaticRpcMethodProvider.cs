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

		public RpcRouteMetaData Get()
		{
			return this.dataAccessor.Value!;
		}
	}

	internal class StaticRpcMethodDataAccessor
	{
		public RpcRouteMetaData? Value { get; set; }
	}
}