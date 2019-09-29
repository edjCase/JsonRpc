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

		public IDictionary<RpcPath, IList<MethodInfo>> Methods { get; }

		public DefaultRouteContext(IServiceProvider serviceProvider, ClaimsPrincipal user, IDictionary<RpcPath, IList<MethodInfo>> methods)
		{
			this.RequestServices = serviceProvider;
			this.User = user;
			this.Methods = methods;
		}

		public static IRouteContext FromHttpContext(HttpContext httpContext, IDictionary<RpcPath, IList<MethodInfo>> methods)
		{
			return new DefaultRouteContext(httpContext.RequestServices, httpContext.User, methods);
		}
	}
}
