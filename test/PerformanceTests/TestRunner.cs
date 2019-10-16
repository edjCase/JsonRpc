using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Common.Tools;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public static async Task RunInvokerAsync()
		{
			var authorizationService = new FakeAuthorizationService();
			var policyProvider = new FakePolicyProvider();
			var logger = new FakeLogger();
			var options = Options.Create(new RpcServerConfiguration());
			const string methodName = "Ping";
			MethodInfo methodInfo = typeof(Controllers.TestController).GetMethod(methodName);
			var info = new RpcMethodInfo(methodInfo, parameters: new object[0]);
			var rpcRequestMatcher = new FakeRequestMatcher(info);
			var invoker = new DefaultRpcInvoker(authorizationService, policyProvider, logger, options, rpcRequestMatcher);

			var request = new RpcRequest(id: null, methodName);
			RpcPath path = "Test";
			var user = new ClaimsPrincipal();
			IServiceProvider serviceProvider = null;
			var methods = new FakeRpcMethodProvider(methodInfo, path);
			var routeContext = new DefaultRouteContext(serviceProvider, user, methods);

            var stopwatch = Stopwatch.StartNew();
            const int total = 1_000_000;
            int onePercent = (int)(total * .01);
            for (int i = 0; i < total; i++)
            {
                await invoker.InvokeRequestAsync(request, routeContext, path);
                if (i % onePercent == 0)
                {
                    Console.WriteLine(i / onePercent);
                }
            }
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
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

			public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
			{
				throw new NotImplementedException();
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
				return false;
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

			public RpcMethodInfo GetMatchingMethod(RpcRequest request, IReadOnlyList<MethodInfo> methods)
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

		public class FakeRpcMethodProvider : IRpcMethodProvider
		{
			private List<MethodInfo> methods { get; }
			private RpcPath path { get; }
			public FakeRpcMethodProvider(MethodInfo info, RpcPath path = null)
			{
				this.methods = new List<MethodInfo> { info };
				this.path = path;
			}

			public bool TryGetByPath(RpcPath path, out IReadOnlyList<MethodInfo> methods)
			{
				if (path != this.path)
				{
					methods = null;
					return false;
				}
				methods = this.methods;
				return true;
			}
		}
	}
}
