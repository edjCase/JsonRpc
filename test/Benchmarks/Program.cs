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
	public class Program
	{
		public static void Main()
		{
			//var tester = new RequestMatcherTester();
			//tester.GlobalSetup();
			//tester.SimpleIterationSetup();
			//Type type = typeof(Program);
			//for (int i = 0; i < 100_000; i++)
			//{
			//	tester.SimpleParamsNoReturn();
			//}
			_ = BenchmarkRunner.Run<RequestMatcherTester>();
		}

		public void Test()
		{

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
			var methodProvider = new FakeMethodProvider();
			var fakeRpcContextAccessor = new FakeRpcContextAccessor();
			this.requestMatcher = new DefaultRequestMatcher(logger, fakeRpcContextAccessor, methodProvider);
		}

		private RpcRequestSignature requestsignature;

		[IterationSetup(Target = nameof(NoParamsNoReturn))]
		public void IterationSetup()
		{
			this.requestsignature = RpcRequestSignature.Create(nameof(MethodClass.NoParamsNoReturn));
		}

		[Benchmark]
		public void NoParamsNoReturn()
		{
			this.requestMatcher.GetMatchingMethod(requestsignature);
		}

		[IterationSetup(Target = nameof(ComplexParamNoReturn))]
		public void ComplexIterationSetup()
		{
			this.requestsignature = RpcRequestSignature.Create(nameof(MethodClass.ComplexParamNoReturn), new[] { RpcParameterType.Object });
		}

		[Benchmark]
		public void ComplexParamNoReturn()
		{
			this.requestMatcher.GetMatchingMethod(requestsignature);
		}


		[IterationSetup(Target = nameof(SimpleParamsNoReturn))]
		public void SimpleIterationSetup()
		{
			var parameters = new Dictionary<string, RpcParameterType>
			{
				{"a", RpcParameterType.Number },
				{"b", RpcParameterType.Boolean },
				{"c", RpcParameterType.String }
			};
			this.requestsignature = RpcRequestSignature.Create(nameof(MethodClass.SimpleParamsNoReturn), parameters);
		}

		[Benchmark]
		public void SimpleParamsNoReturn()
		{
			this.requestMatcher.GetMatchingMethod(requestsignature);
		}


#pragma warning disable IDE0060 // Remove unused parameter
		internal class MethodClass
		{
			public void NoParamsNoReturn()
			{

			}
			public void ComplexParamNoReturn(ComplexParam complex)
			{

			}

			public void SimpleParamsNoReturn(int a, bool b, string c)
			{

			}

			public class ComplexParam
			{
				public string A { get; set; }
				public bool B { get; set; }
				public ComplexParam C { get; set; }
			}
		}
#pragma warning restore IDE0060 // Remove unused parameter
	}

	public class FakeRpcContextAccessor : IRpcContextAccessor
	{
		IRpcContext? IRpcContextAccessor.Value { get; set; } = new DefaultRpcContext(null, "/api/v1/controller_name");
	}

	internal class FakeMethodProvider : IRpcMethodProvider
	{
		private static readonly List<MethodInfo> methods = typeof(RequestMatcherTester.MethodClass)
			.GetTypeInfo()
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(m => m.DeclaringType != typeof(object))
			.ToList();

		public IReadOnlyList<MethodInfo> Get()
		{
			return FakeMethodProvider.methods;
		}
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
