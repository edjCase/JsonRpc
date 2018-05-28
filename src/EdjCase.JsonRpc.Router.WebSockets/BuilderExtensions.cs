using System;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.RouteProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EdjCase.JsonRpc.Router.WebSockets
{
	public static class BuilderExtensions
	{
		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="configureOptions">Action to configure routing</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpcWithWebSockets(this IApplicationBuilder app, Action<SingleRouteOptions> configureOptions)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (configureOptions == null)
			{
				throw new ArgumentNullException(nameof(configureOptions));
			}
			var options = new SingleRouteOptions();
			configureOptions(options);
			return app.UseJsonRpcWithWebSockets(options);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="configureOptions">Action to configure routing</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpcWithWebSockets(this IApplicationBuilder app, SingleRouteOptions options)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}
			IRpcRouteProvider routeProvider = new RpcSingleRouteProvider(Options.Create(options));
			return app
			.UseWebSockets()
			.UseJsonRpcWithWebSockets(routeProvider);
		}



		public static IApplicationBuilder UseJsonRpcWithWebSockets(this IApplicationBuilder app, IRpcRouteProvider routeProvider)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (routeProvider == null)
			{
				throw new ArgumentNullException(nameof(routeProvider));
			}
			if (app.ApplicationServices.GetService<RpcServicesMarker>() == null)
			{
				throw new InvalidOperationException("AddJsonRpc() needs to be called in the ConfigureServices method.");
			}
			var router = new RpcWebSocketRouter(routeProvider);
			return app.UseRouter(router);
		}
	}
}