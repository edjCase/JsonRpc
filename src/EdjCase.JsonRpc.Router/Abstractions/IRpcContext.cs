using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;

public interface IRpcContext
{
	IServiceProvider RequestServices { get; }
	ClaimsPrincipal User { get; }
	RpcPath? Path { get; }
}

public interface IRpcContextAccessor
{
	IRpcContext Value { get; set; }
}

public interface IRpcMethodProvider
{
	IReadOnlyList<MethodInfo> Get();
}