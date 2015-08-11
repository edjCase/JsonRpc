using JsonRpc.Router;
using System;
using JsonRpc.Router.Abstractions;
using Microsoft.Framework.DependencyInjection;
using JsonRpc.Router.Defaults;
using Microsoft.Framework.Logging;

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
			ILoggerFactory loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
			app.UseRouter(new RpcRouter(configuration, rpcInvoker, rpcParser, rpcCompressor, loggerFactory));
		}

		public static void AddJsonRpc(this IServiceCollection serviceCollection)
		{
			serviceCollection.AddSingleton<IRpcInvoker, DefaultRpcInvoker>(sp =>
			{
				ILoggerFactory loggerrFactory = sp.GetService<ILoggerFactory>();
				ILogger logger = loggerrFactory?.CreateLogger("Json Rpc Invoker");
				return new DefaultRpcInvoker(logger);
			});
			serviceCollection.AddSingleton<IRpcParser, DefaultRpcParser>(sp =>
			{
				ILoggerFactory loggerrFactory = sp.GetService<ILoggerFactory>();
				ILogger logger = loggerrFactory?.CreateLogger("Json Rpc Parser");
				return new DefaultRpcParser(logger);
			});
			serviceCollection.AddSingleton<IRpcCompressor, DefaultRpcCompressor>(sp =>
			{
				ILoggerFactory loggerrFactory = sp.GetService<ILoggerFactory>();
				ILogger logger = loggerrFactory?.CreateLogger("Json Rpc Compressor");
				return new DefaultRpcCompressor(logger);
			});
		}
	}
}