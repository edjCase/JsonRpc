using System;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EdjCase.JsonRpc.Common.Tools;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
	/// <summary>
	/// Extension class to add JsonRpc router to Asp.Net pipeline
	/// </summary>
	public static class BuilderExtensions
	{
		/// <summary>
		/// Extension method to add the JsonRpc router services to the IoC container
		/// </summary>
		/// <param name="serviceCollection">IoC serivce container to register JsonRpc dependencies</param>
		/// <param name="configureOptions">Configuration action for server wide rpc configuration</param>
		/// <returns>IoC service container</returns>
		public static IServiceCollection AddJsonRpc(this IServiceCollection serviceCollection, Action<RpcServerConfiguration>? configureOptions)
		{
			var configuration = new RpcServerConfiguration();
			configureOptions?.Invoke(configuration);
			return serviceCollection.AddJsonRpc(configuration);
		}

		/// <summary>
		/// Extension method to add the JsonRpc router services to the IoC container
		/// </summary>
		/// <param name="serviceCollection">IoC serivce container to register JsonRpc dependencies</param>
		/// <param name="configuration">(Optional) Server wide rpc configuration</param>
		/// <returns>IoC service container</returns>
		public static IServiceCollection AddJsonRpc(this IServiceCollection serviceCollection, RpcServerConfiguration? configuration = null)
		{
			if (serviceCollection == null)
			{
				throw new ArgumentNullException(nameof(serviceCollection));
			}

			serviceCollection.AddSingleton(new RpcServicesMarker());
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
				.TryAddScoped<IRpcRequestMatcher, DefaultRequestMatcher>();
			serviceCollection
				.TryAddScoped<IRpcContextAccessor, DefaultContextAccessor>();
			serviceCollection
				.TryAddScoped<IRpcAuthorizationHandler, DefaultAuthorizationHandler>();
			serviceCollection
				.TryAddScoped<IRpcMethodProvider, StaticRpcMethodProvider>();
			serviceCollection
				.TryAddScoped<IRpcParameterConverter, DefaultRpcParameterConverter>();
			serviceCollection
				.TryAddSingleton<StaticRpcMethodDataAccessor>();
			serviceCollection.AddHttpContextAccessor();

			if (configuration == null)
			{
				configuration = new RpcServerConfiguration();
			}
			return serviceCollection
				.AddSingleton(Options.Create(configuration))
				.AddRouting()
				.AddAuthorizationCore();
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="builder">(Optional) Action to configure the endpoints. Will default to include all controllers that derive from `RpcController`</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, Action<RpcEndpointBuilder>? builder = null)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			var options = new RpcEndpointBuilder();
			builder = builder ?? BuildFromBaseController<RpcController>;
			builder.Invoke(options);
			RpcRouteMetaData data = options.Resolve();
			return app.UseJsonRpc(data);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// Uses all the public methods on controllers extending the specified class
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpcWithBaseController<T>(this IApplicationBuilder app)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			return app.UseJsonRpc(BuildFromBaseController<T>);
		}

		private static void BuildFromBaseController<T>(RpcEndpointBuilder builder)
		{
			Type baseControllerType = typeof(T);
			IEnumerable<Type> controllers = Assembly
				.GetEntryAssembly()!
				.GetReferencedAssemblies()
				.Select(Assembly.Load)
				.SelectMany(x => x.DefinedTypes)
				.Concat(Assembly.GetEntryAssembly()!.DefinedTypes)
				.Where(t => !t.IsAbstract && (t == baseControllerType || t.IsSubclassOf(baseControllerType)));

			foreach (Type type in controllers)
			{
				builder.AddController(type);
			}
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="methodProvider">All the available methods to call</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		internal static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, RpcRouteMetaData data)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}
			if (app.ApplicationServices.GetService<RpcServicesMarker>() == null)
			{
				throw new InvalidOperationException("AddJsonRpc() needs to be called in the ConfigureServices method.");
			}
			var router = new RpcHttpRouter();
			app.ApplicationServices.GetRequiredService<StaticRpcMethodDataAccessor>().Value = data;
			return app.UseRouter(router);
		}


	}

	internal class RpcServicesMarker
	{

	}
}