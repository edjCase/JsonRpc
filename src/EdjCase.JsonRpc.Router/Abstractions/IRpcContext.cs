using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcContext
	{
		IServiceProvider RequestServices { get; }
		RpcPath? Path { get; }
	}

	internal interface IRpcContextAccessor
	{
		IRpcContext? Value { get; set; }
	}
}