using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public class RpcContext
	{
		public IServiceProvider RequestServices { get; }
		public RpcPath? Path { get; }

		public RpcContext(IServiceProvider serviceProvider, RpcPath? path = null)
		{
			this.RequestServices = serviceProvider;
			this.Path = path;
		}
	}

	internal interface IRpcContextAccessor
	{
		RpcContext Get();
		void Set(RpcContext context);
	}
}