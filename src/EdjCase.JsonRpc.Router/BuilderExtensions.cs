using System;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EdjCase.JsonRpc.Common.Tools;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Reflection;
using EdjCase.JsonRpc.Router.Utilities;
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
		/// <returns>IoC service container</returns>
		public static IRpcBuilder AddJsonRpc(this IServiceCollection serviceCollection)
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
				.AddRouting()
				.AddAuthorizationCore();

			return new RpcBuilder(serviceCollection);
		}


		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// Uses all the public methods on controllers extending <see cref="RpcController"/>
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			return app.UseJsonRpcWithBaseController<RpcController>();
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

			return app.UseJsonRpc(builder =>
			{
				Type baseControllerType = typeof(T);
				IEnumerable<Type> controllers = Assembly
					.GetEntryAssembly()
					.GetReferencedAssemblies()
					.Select(Assembly.Load)
					.SelectMany(x => x.DefinedTypes)
					.Concat(Assembly.GetEntryAssembly().DefinedTypes)
					.Where(t => !t.IsAbstract && (t == baseControllerType || t.IsSubclassOf(baseControllerType)));

				foreach (Type type in controllers)
				{
					builder.AddControllerWithDefaultPath(type);
				}
			});
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="builder">Action to configure the endpoints</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		public static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, Action<RpcEndpointBuilder> builder)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			var options = new RpcEndpointBuilder();
			builder.Invoke(options);
			StaticRpcMethodData data = options.Resolve();
			return app.UseJsonRpc(data);
		}

		/// <summary>
		/// Extension method to use the JsonRpc router in the Asp.Net pipeline
		/// </summary>
		/// <param name="app"><see cref="IApplicationBuilder"/> that is supplied by Asp.Net</param>
		/// <param name="methodProvider">All the available methods to call</param>
		/// <returns><see cref="IApplicationBuilder"/> that includes the Basic auth middleware</returns>
		internal static IApplicationBuilder UseJsonRpc(this IApplicationBuilder app, StaticRpcMethodData data)
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
			return app
				.Use((context, next) =>
				{
					context.RequestServices.GetRequiredService<StaticRpcMethodDataAccessor>().Value = data;
					return next();
				})
				.UseRouter(router);
		}

	}

	public interface IRpcBuilder
	{
		IServiceCollection Services { get; }
	}

	internal class RpcBuilder : IRpcBuilder
	{
		public IServiceCollection Services { get; }

		public RpcBuilder(IServiceCollection services)
		{
			this.Services = services;
		}
	}

	public class RpcServicesMarker
	{

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
			builder.Services.AddSingleton(Options.Create(configuration));
			return builder;
		}

		public static IRpcBuilder WithParser<T>(this IRpcBuilder builder)
			where T : class, IRpcParser
		{
			builder.Services.AddScoped<IRpcParser, T>();
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

		public static IRpcBuilder WithRequestMatcher<T>(this IRpcBuilder builder)
			where T : class, IRpcRequestMatcher
		{
			builder.Services.AddScoped<IRpcRequestMatcher, T>();
			return builder;
		}
	}
}