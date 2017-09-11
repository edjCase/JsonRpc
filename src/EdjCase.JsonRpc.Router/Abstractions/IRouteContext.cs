using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Security.Claims;

public interface IRouteContext
{
	IServiceProvider RequestServices { get; }
	IRpcRouteProvider RouteProvider { get; }
	ClaimsPrincipal User { get; }
}