using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;

public interface IRouteContext
{
	IServiceProvider RequestServices { get; }
	ClaimsPrincipal User { get; }
	IDictionary<RpcPath, IList<MethodInfo>> Methods { get; }
}