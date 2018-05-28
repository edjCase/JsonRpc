using System;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using EdjCase.JsonRpc.Router.RouteProviders;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EdjCase.JsonRpc.Core.Tools;

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
		/// <param name="configureOptions">Optional action to configure auto routing</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, Action<RpcAutoRoutingOptions> configureOptions = null)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			var options = new RpcAutoRoutingOptions();
			configureOptions?.Invoke(options);
			IRpcRouteProvider routeProvider = new RpcAutoRouteProvider(Options.Create(options));
			return app.UseJsonRpc(routeProvider);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="options">Auto routing configuration</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, RpcAutoRoutingOptions options)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}
			IRpcRouteProvider routeProvider = new RpcAutoRouteProvider(Options.Create(options));
			return app.UseJsonRpc(routeProvider);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="options">Auto routing configuration</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, IRpcRouteProvider routeProvider)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (routeProvider == null)
			{
				throw new ArgumentNullException(nameof(routeProvider));
			}
			var router = new RpcRouter(routeProvider);
			return app.UseRouter(router);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="configureOptions">Optional action to configure manual routing</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseManualJsonRpc(this IApplicationBuilder app, Action<RpcManualRoutingOptions> configureOptions = null)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			var options = new RpcManualRoutingOptions();
			configureOptions?.Invoke(options);
			IRpcRouteProvider routeProvider = new RpcManualRouteProvider(Options.Create(options));
			return app.UseJsonRpc(routeProvider);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="options">Manual routing configuration</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseManualJsonRpc(this IApplicationBuilder app, RpcManualRoutingOptions options)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}
			IRpcRouteProvider routeProvider = new RpcManualRouteProvider(Options.Create(options));
			return app.UseJsonRpc(routeProvider);
		}


		/// <summary>
		/// Extension method to add the JsonRpc router services to the IoC container
		/// </summary>
		/// <param name="serviceCollection">IoC serivce container to register JsonRpc dependencies</param>
		/// <returns>IoC service container</returns>
		public static IRpcBuilder AddJsonRpc(this IServiceCollection serviceCollection)
		{
			if (serviceCollection == null)
			{
				throw new ArgumentNullException(nameof(serviceCollection));
			}

			serviceCollection
				.TryAddScoped<IRpcInvoker, DefaultRpcInvoker>();
			serviceCollection
				.TryAddScoped<IRpcParser, DefaultRpcParser>();
			serviceCollection
				.TryAddScoped<IRpcRequestHandler, RpcRequestHandler>();
			serviceCollection
				.TryAddScoped<IStreamCompressor, DefaultStreamCompressor>();
			serviceCollection
				.TryAddScoped<IRpcResponseSerializer, DefaultRpcResponseSerializer>();
			serviceCollection
				.TryAddScoped<IRpcRouteProvider, RpcAutoRouteProvider>();
			serviceCollection
				.TryAddScoped<IRpcRequestMatcher, DefaultRequestMatcher>();

			serviceCollection
				.AddRouting()
				.AddAuthorization();

			return new RpcBuilder(serviceCollection);
		}
	}

	public interface IRpcBuilder
	{
		IServiceCollection Services { get; }
	}

	public class RpcBuilder : IRpcBuilder
	{
		public IServiceCollection Services { get; }

		public RpcBuilder(IServiceCollection services)
		{
			this.Services = services;
		}
	}

	public static class RpcBuilderExtensions
	{
		public static IRpcBuilder WithOptions(this IRpcBuilder builder, Action<RpcServerConfiguration> configureOptions)
		{
			var configuration = new RpcServerConfiguration();
			configureOptions?.Invoke(configuration);
			builder.Services.Configure(configureOptions);
			return builder;
		}

		public static IRpcBuilder WithOptions(this IRpcBuilder builder, RpcServerConfiguration configuration)
		{
			builder.Services.AddSingleton<IOptions<RpcServerConfiguration>>(Options.Create(configuration));
			return builder;
		}

		public static IRpcBuilder WithInvoker<T>(this IRpcBuilder builder)
			where T : class, IRpcInvoker
		{
			builder.Services.AddScoped<IRpcInvoker, T>();
			return builder;
		}

		public static IRpcBuilder WithParser<T>(this IRpcBuilder builder)
			where T : class, IRpcParser
		{
			builder.Services.AddScoped<IRpcParser, T>();
			return builder;
		}

		public static IRpcBuilder WithRequestHanlder<T>(this IRpcBuilder builder)
			where T : class, IRpcRequestHandler
		{
			builder.Services.AddScoped<IRpcRequestHandler, T>();
			return builder;
		}

		public static IRpcBuilder WithCompressor<T>(this IRpcBuilder builder)
			where T : class, IStreamCompressor
		{
			builder.Services.AddScoped<IStreamCompressor, T>();
			return builder;
		}

		public static IRpcBuilder WithReponseSerializer<T>(this IRpcBuilder builder)
			where T : class, IRpcResponseSerializer
		{
			builder.Services.AddScoped<IRpcResponseSerializer, T>();
			return builder;
		}

		public static IRpcBuilder WithRouteProvider<T>(this IRpcBuilder builder)
			where T : class, IRpcRouteProvider
		{
			builder.Services.AddScoped<IRpcRouteProvider, T>();
			return builder;
		}

		public static IRpcBuilder WithRequestMatcher<T>(this IRpcBuilder builder)
			where T : class, IRpcRequestMatcher
		{
			builder.Services.AddScoped<IRpcRequestMatcher, T>();
			return builder;
		}
	}
}