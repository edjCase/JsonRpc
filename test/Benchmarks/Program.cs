using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benchmarks
{
	public static class Program
	{
		public static void Main()
		{
			BenchmarkDotNet.Reports.Summary summary = BenchmarkRunner.Run<RequestMatcherTester>();
		}
	}

	[MValueColumn]
	[MemoryDiagnoser]
	[SimpleJob(RunStrategy.Throughput, invocationCount: 100_000)]
	public class RequestMatcherTester
	{
		private IRpcRequestMatcher requestMatcher;

		[GlobalSetup]
		public void GlobalSetup()
		{
			var logger = new FakeLogger<DefaultRequestMatcher>();
			var contextAccessor = new FakeContextAccessor();
			var methodProvider = new FakeMethodProvider();
			this.requestMatcher = new DefaultRequestMatcher(logger, contextAccessor, methodProvider);
		}

		private RpcRequestSignature requestsignature;

		[IterationSetup(Target = nameof(NoParamsNoReturn))]
		public void IterationSetup()
		{
			this.requestsignature = RpcRequestSignature.Create(new RpcRequest(1, "NoParamsNoReturn"));
		}

		[Benchmark]
		public void NoParamsNoReturn()
		{
			this.requestMatcher.GetMatchingMethod(requestsignature);
		}

		[IterationSetup(Target = nameof(ComplexParamNoReturn))]
		public void ComplexIterationSetup()
		{
			var complexParam = new MethodClass.ComplexParam
			{
				A = "Test",
				B = true,
				C = new MethodClass.ComplexParam
				{
					A = "Test2",
					B = false,
					C = null
				}
			};
			var param = new RawRpcParameter(RpcParameterType.Object, complexParam);
			this.requestsignature = RpcRequestSignature.Create(new RpcRequest(1, "ComplexParamNoReturn", new RpcParameters(param)));
		}

		[Benchmark]
		public void ComplexParamNoReturn()
		{
			this.requestMatcher.GetMatchingMethod(requestsignature);
		}


		[IterationSetup(Target = nameof(SimpleParamsNoReturn))]
		public void SimpleIterationSetup()
		{
			var parameters = new Dictionary<string, IRpcParameter>
			{
				{"a",  new RawRpcParameter(RpcParameterType.Number, 1) },
				{"b",  new RawRpcParameter(RpcParameterType.Boolean, true) },
				{"c",  new RawRpcParameter(RpcParameterType.String, "Test") }
			};
			this.requestsignature = RpcRequestSignature.Create(new RpcRequest(1, "SimpleParamsNoReturn", new RpcParameters(parameters)));
		}

		[Benchmark]
		public void SimpleParamsNoReturn()
		{
			this.requestMatcher.GetMatchingMethod(requestsignature);
		}


		internal class MethodClass
		{
			public void NoParamsNoReturn()
			{

			}
#pragma warning disable IDE0060 // Remove unused parameter
			public void ComplexParamNoReturn(ComplexParam complex)
#pragma warning restore IDE0060 // Remove unused parameter
			{

			}

#pragma warning disable IDE0060 // Remove unused parameter
			public void SimpleParamsNoReturn(int a, bool b, string c)
#pragma warning restore IDE0060 // Remove unused parameter
			{

			}

			public class ComplexParam
			{
				public string A { get; set; }
				public bool B { get; set; }
				public ComplexParam C { get; set; }
			}
		}
	}

	internal class FakeMethodProvider : IRpcMethodProvider
	{
		private List<MethodInfo> methods;
		public FakeMethodProvider()
		{
			this.methods = typeof(RequestMatcherTester.MethodClass).GetTypeInfo().GetMethods().ToList();
		}

		public IReadOnlyList<MethodInfo> Get()
		{
			return this.methods;
		}
	}

	internal class FakeContextAccessor : IRpcContextAccessor
	{
		public IRpcContext Value { get; set; } = new FakeRpcContext();
	}

	internal class FakeRpcContext : IRpcContext
	{
		public IServiceProvider RequestServices => throw new NotImplementedException();

		public ClaimsPrincipal User => throw new NotImplementedException();

		public RpcPath Path { get; } = null;
	}

	public class FakeLogger<T> : ILogger<T>
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

		private class FakeDisposable : IDisposable
		{
			public void Dispose()
			{

			}
		}
	}
}
