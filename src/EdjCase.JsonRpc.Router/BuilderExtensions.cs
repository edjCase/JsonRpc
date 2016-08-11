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
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			
			RpcRouter router = ActivatorUtilities.CreateInstance<RpcRouter>(app.ApplicationServices);
			return app.UseRouter(router);
		}

		/// <summary>
		/// Extension method to add the JsonRpc router services to the IoC container
		/// </summary>
		/// <param name="serviceCollection">IoC serivce container to register JsonRpc dependencies</param>
		/// <param name="configureOptions">Action to configure the router properties</param>
		/// <returns>IoC service container</returns>
		public static IServiceCollection AddJsonRpc(this IServiceCollection serviceCollection, Action<RpcRouterConfiguration> configureOptions)
		{
			if (configureOptions == null)
			{
				throw new ArgumentNullException(nameof(configureOptions));
			}

			RpcRouterConfiguration configuration = new RpcRouterConfiguration();
			configureOptions.Invoke(configuration);
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
			return serviceCollection
				.AddRouting()
				.AddAuthorization()
				.AddSingleton(Options.Create(configuration))
				.AddSingleton<IRpcInvoker, DefaultRpcInvoker>()
				.AddSingleton<IRpcParser, DefaultRpcParser>()
				.AddSingleton<IRpcCompressor, DefaultRpcCompressor>();
		}
	}
}