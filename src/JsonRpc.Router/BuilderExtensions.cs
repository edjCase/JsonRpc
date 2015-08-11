using JsonRpc.Router;
using System;
using JsonRpc.Router.Abstractions;
using Microsoft.Framework.DependencyInjection;

// ReSharper disable once CheckNamespace
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

			IRpcInvoker rpcInvoker = app.ApplicationServices.GetRequiredService<IRpcInvoker>();
			IRpcParser rpcParser = app.ApplicationServices.GetRequiredService<IRpcParser>();
			IRpcCompressor rpcCompressor = app.ApplicationServices.GetRequiredService<IRpcCompressor>();
			app.UseRouter(new RpcRouter(configuration, rpcInvoker, rpcParser, rpcCompressor));
		}

		public static void AddJsonRpc(this IServiceCollection serviceCollection)
		{
			serviceCollection.AddSingleton<IRpcInvoker, DefaultRpcInvoker>();
			serviceCollection.AddSingleton<IRpcParser, DefaultRpcParser>();
			serviceCollection.AddSingleton<IRpcCompressor, DefaultRpcCompressor>();
		}
	}
}