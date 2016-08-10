using System;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
	/// <summary>
	/// Extension class to add JsonRpc router to Asp.Net pipeline
	/// </summary>
	public static class BuilderExtensions
	{
		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="configureRouter">Action to configure the router properties</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, Action<RpcRouterConfiguration> configureRouter)
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
#if !NETSTANDARD1_3
			if (configuration.AutoRegisterControllers)
			{
				configuration.Routes.AddControllerRoutes();
			}
#endif
			if (configuration.Routes.Count < 1)
			{
				throw new RpcConfigurationException("At least on class/route must be configured for router to work.");
			}
			
			RpcRouter router = ActivatorUtilities.CreateInstance<RpcRouter>(app.ApplicationServices, Options.Create(configuration));
			return app.UseRouter(router);
		}

		/// <summary>
		/// Extension method to add the JsonRpc router services to the IoC container
		/// </summary>
		/// <param name="serviceCollection">IoC serivce container to register JsonRpc dependencies</param>
		/// <returns>IoC service container</returns>
		public static IServiceCollection AddJsonRpc(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddRouting()
				.AddAuthorization()
				.AddSingleton<IRpcInvoker, DefaultRpcInvoker>()
				.AddSingleton<IRpcParser, DefaultRpcParser>()
				.AddSingleton<IRpcCompressor, DefaultRpcCompressor>();
		}
	}
}