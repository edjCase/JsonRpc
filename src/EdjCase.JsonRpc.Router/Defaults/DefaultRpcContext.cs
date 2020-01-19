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
	internal class DefaultRpcContext : IRpcContext
	{
		public IServiceProvider RequestServices { get; }

		public ClaimsPrincipal User { get; }

		public RpcPath? Path { get; }

		public DefaultRpcContext(IServiceProvider serviceProvider, ClaimsPrincipal user, RpcPath? path = null)
		{
			this.RequestServices = serviceProvider;
			this.User = user;
			this.Path = path;
		}

		public static IRpcContext FromHttpContext(HttpContext httpContext, RpcPath? path = null)
		{
			return new DefaultRpcContext(httpContext.RequestServices, httpContext.User, path);
		}
	}
}
