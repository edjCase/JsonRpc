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
		/// <param name="configureProvider">Action to configure route provider</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, Action<IRpcRouteProvider> configureProvider = null)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			IRpcRouteProvider routeProvider = new RpcRouteProvider();
			configureProvider?.Invoke(routeProvider);

			RpcRouter router = ActivatorUtilities.CreateInstance<RpcRouter>(app.ApplicationServices, routeProvider);
			return app.UseRouter(router);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="routeProvider">Action to configure route provider</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, IRpcRouteProvider routeProvider)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if(routeProvider == null)
			{
				throw new ArgumentNullException(nameof(routeProvider));
			}

			RpcRouter router = ActivatorUtilities.CreateInstance<RpcRouter>(app.ApplicationServices, routeProvider);
			return app.UseRouter(router);
		}

		/// <summary>
		/// Extension method to add the JsonRpc router services to the IoC container
		/// </summary>
		/// <param name="serviceCollection">IoC serivce container to register JsonRpc dependencies</param>
		/// <param name="configureOptions">Action to configure the router properties</param>
		/// <returns>IoC service container</returns>
		public static IServiceCollection AddJsonRpc(this IServiceCollection serviceCollection, Action<RpcServerConfiguration> configureOptions = null)
		{
			if (serviceCollection == null)
			{
				throw new ArgumentNullException(nameof(serviceCollection));
			}
			RpcServerConfiguration configuration = new RpcServerConfiguration();
			configureOptions?.Invoke(configuration);

			return serviceCollection
				.AddRouting()
				.AddAuthorization()
				.Configure<RpcServerConfiguration>(configureOptions ?? (options => { }))
				.AddSingleton<IRpcInvoker, DefaultRpcInvoker>()
				.AddSingleton<IRpcParser, DefaultRpcParser>()
				.AddSingleton<IRpcCompressor, DefaultRpcCompressor>();
		}
	}
}