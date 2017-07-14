using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EdjCase.JsonRpc.Router.Defaults
{
	public class DefaultRouteContext : IRouteContext
	{
		public IServiceProvider RequestServices { get; }

		public ClaimsPrincipal User { get; }

		public IRpcRouteProvider RouteProvider { get; }

		public DefaultRouteContext(IServiceProvider serviceProvider, ClaimsPrincipal user, IRpcRouteProvider routeProvider)
		{
			this.RequestServices = serviceProvider;
			this.User = user;
			this.RouteProvider = routeProvider;
		}

		public static IRouteContext FromHttpContext(HttpContext httpContext, IRpcRouteProvider routeProvider)
		{
			return new DefaultRouteContext(httpContext.RequestServices, httpContext.User, routeProvider);
		}
	}
}
