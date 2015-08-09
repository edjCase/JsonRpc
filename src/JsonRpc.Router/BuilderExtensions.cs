using JsonRpc.Router;
using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Builder
{
	public static class BuilderExtensions
	{
		public static void UseJsonRpc(this IApplicationBuilder app, Action<RpcRouterConfiguration> configureRouter)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (configureRouter == null)
			{
				throw new ArgumentNullException(nameof(configureRouter));
			}

			RpcRouterConfiguration configuration = new RpcRouterConfiguration();
			configureRouter.Invoke(configuration);

			app.UseRouter(new RpcRouter(configuration));
		}
	}
}