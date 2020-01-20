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
			var methodProvider = new FakeMethodProvider();
			this.requestMatcher = new DefaultRequestMatcher(logger, methodProvider);
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

	internal class FakeMethodProvider : IRpcMethodProvider
	{
		private static MethodInfo[] methods = typeof(RequestMatcherTester.MethodClass).GetTypeInfo().GetMethods().ToArray();

		public MethodInfo[] Get()
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
