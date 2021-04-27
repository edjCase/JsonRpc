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

#pragma warning disable CA1822 // Mark members as static
		public void Test()
#pragma warning restore CA1822 // Mark members as static
		{

		}
	}

	[MValueColumn]
	[MemoryDiagnoser]
	[SimpleJob(RunStrategy.Throughput, invocationCount: 100_000)]
	public class RequestMatcherTester
	{
		private IRpcRequestMatcher? requestMatcher;

		[GlobalSetup]
		public void GlobalSetup()
		{
			var logger = new FakeLogger<DefaultRequestMatcher>();
			var methodProvider = new FakeMethodProvider();
			var fakeRpcContextAccessor = new FakeRpcContextAccessor();
			this.requestMatcher = new DefaultRequestMatcher(logger, methodProvider, fakeRpcContextAccessor);
		}

		private RpcRequestSignature? requestsignature;

		[IterationSetup(Target = nameof(NoParamsNoReturn))]
		public void IterationSetup()
		{
			this.requestsignature = RpcRequestSignature.Create(nameof(MethodClass.NoParamsNoReturn));
		}

		[Benchmark]
		public void NoParamsNoReturn()
		{
			this.requestMatcher!.GetMatchingMethod(requestsignature!);
		}

		[IterationSetup(Target = nameof(ComplexParamNoReturn))]
		public void ComplexIterationSetup()
		{
			this.requestsignature = RpcRequestSignature.Create(nameof(MethodClass.ComplexParamNoReturn), new[] { RpcParameterType.Object });
		}

		[Benchmark]
		public void ComplexParamNoReturn()
		{
			this.requestMatcher!.GetMatchingMethod(requestsignature!);
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
			this.requestMatcher!.GetMatchingMethod(requestsignature!);
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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
				public string A { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
				public bool B { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
				public ComplexParam C { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
			}
		}
#pragma warning restore IDE0060 // Remove unused parameter
	}

	public class FakeRpcContextAccessor : IRpcContextAccessor
	{

		public RpcContext Get()
		{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			return new RpcContext(null, "/api/v1/controller_name");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		}

		public void Set(RpcContext context)
		{
			throw new NotImplementedException();
		}
	}

	internal class FakeMethodProvider : IRpcMethodProvider
	{
		private static readonly List<DefaultRpcMethodInfo> methods = typeof(RequestMatcherTester.MethodClass)
			.GetTypeInfo()
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(m => m.DeclaringType != typeof(object))
			.Select(DefaultRpcMethodInfo.FromMethodInfo)
			.ToList();


		public RpcRouteMetaData Get()
		{
			return new RpcRouteMetaData(FakeMethodProvider.methods, new Dictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>>());
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
