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
	public class DefaultRouteContext : IRouteContext
	{
		public IServiceProvider RequestServices { get; }

		public ClaimsPrincipal User { get; }

		public IRpcMethodProvider MethodProvider { get; }

		public DefaultRouteContext(IServiceProvider serviceProvider, ClaimsPrincipal user, IRpcMethodProvider methodProvider)
		{
			this.RequestServices = serviceProvider;
			this.User = user;
			this.MethodProvider = methodProvider;
		}

		public static IRouteContext FromHttpContext(HttpContext httpContext, IRpcMethodProvider methodProvider)
		{
			return new DefaultRouteContext(httpContext.RequestServices, httpContext.User, methodProvider);
		}
	}
}
