using Edjcase.JsonRpc.Router;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Core.Tools;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using EdjCase.JsonRpc.Router.RouteProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTests
{
	public static class TestRunner
	{

		public static void RunCompression()
		{
			var compressor = new DefaultStreamCompressor();
			const string text = "df;lkajsd;flkja;lksdjf;lkajsd;lkfjl;aksjdfl;kjas;kldjfkl;ajsd;lkfjalk;sdjflk;ajsd;klfjal;ksdjfl;kajsdklf;j";
			using (Stream inputStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
			{
				for (int i = 0; i < 1_000_000; i++)
				{
					using (MemoryStream compressedStream = new MemoryStream((int)inputStream.Length))
					{
						compressor.Compress(inputStream, compressedStream, CompressionType.Gzip);
						compressedStream.Position = 0;
						using (MemoryStream uncompressedStream = new MemoryStream((int)inputStream.Length))
						{
							compressor.Decompress(compressedStream, uncompressedStream, CompressionType.Gzip);
						}
					}
				}
			}
		}
		public static async Task RunInvokerAsync()
		{
			var authorizationService = new FakeAuthorizationService();
			var policyProvider = new FakePolicyProvider();
			var logger = new FakeLogger();
			var options = Options.Create(new RpcServerConfiguration());
			const string methodName = "Ping";
			MethodInfo methodInfo = typeof(Controllers.TestController).GetMethod(methodName);
			var info = new EdjCase.JsonRpc.Router.RpcMethodInfo(methodInfo, parameters: new object[0]);
			var rpcRequestMatcher = new FakeRequestMatcher(info);
			var invoker = new DefaultRpcInvoker(authorizationService, policyProvider, logger, options, rpcRequestMatcher);

			var request = new RpcRequest(id: null, methodName);
			const string path = "Test";
			var routingOptions = new RpcAutoRoutingOptions();
			var routeProvider = new RpcAutoRouteProvider(Options.Create(routingOptions));
			var user = new ClaimsPrincipal();
			IServiceProvider serviceProvider = null;
			var routeContext = new DefaultRouteContext(serviceProvider, user, routeProvider);
			for (int i = 0; i < 10_000_000; i++)
			{
				await invoker.InvokeRequestAsync(request, routeContext, path);
			}
		}

		private class FakeAuthorizationService : IAuthorizationService
		{
			public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
			{
				return Task.FromResult(AuthorizationResult.Success());
			}

			public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
			{
				return Task.FromResult(AuthorizationResult.Success());
			}
		}

		private class FakePolicyProvider : IAuthorizationPolicyProvider
		{
			public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
			{
				var requirements = new List<IAuthorizationRequirement>
				{
					new FakeAuthorizationRequirement()
				};
				var authenticationSchemes = new List<string> { "Bearer" };
				return Task.FromResult(new AuthorizationPolicy(requirements, authenticationSchemes));
			}

			public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
			{
				throw new NotImplementedException();
			}

			private class FakeAuthorizationRequirement : IAuthorizationRequirement
			{
			}
		}

		private class FakeLogger : ILogger<DefaultRpcInvoker>
		{
			public IDisposable BeginScope<TState>(TState state)
			{
				return new FakeDisposable();
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			{

			}
		}

		private class FakeRequestMatcher : IRpcRequestMatcher
		{
			private RpcMethodInfo method { get; }
			public FakeRequestMatcher(EdjCase.JsonRpc.Router.RpcMethodInfo method)
			{
				this.method = method;
			}

			public RpcMethodInfo GetMatchingMethod(RpcRequest request, List<MethodInfo> methods)
			{
				return this.method;
			}
		}

		public class FakeDisposable : IDisposable
		{
			public void Dispose()
			{

			}
		}
	}
}
