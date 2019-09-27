using System;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EdjCase.JsonRpc.Core.Tools;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using System.Net.WebSockets;

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
				.AddAuthorization();

			return new RpcBuilder(serviceCollection);
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
			builder.Services.AddSingleton<IOptions<RpcServerConfiguration>>(Options.Create(configuration));
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