using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EdjCase.JsonRpc.Router.Defaults
{

	internal class DefaultContextAccessor : IRpcContextAccessor
	{
		private RpcContext? value;

		public RpcContext Get()
		{
			if (this.value == null)
			{
				throw new InvalidOperationException("Cannot access rpc context outside of a rpc request scope");
			}
			return this.value;
		}

		public void Set(RpcContext context)
		{
			if (this.value != null)
			{
				throw new InvalidOperationException("Cannot set rpc context multiple times");
			}
			this.value = context;
		}
	}
}
