using System;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Moq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using EdjCase.JsonRpc.Router;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit.Abstractions;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class InvokerTests
	{
		private readonly ITestOutputHelper _output;

		public InvokerTests(ITestOutputHelper output)
		{
			this._output = output;
		}
		private DefaultRpcInvoker GetInvoker(string? methodName, RpcPath? path = null,
			Action<RpcServerConfiguration>? configure = null)
		{
			MethodInfo? methodInfo = null;
			if (methodName != null)
			{
				methodInfo = typeof(TestRouteClass).GetMethod(methodName)!;
			}
			return this.GetInvoker(methodInfo, path, configure: configure);
		}

		private DefaultRpcInvoker GetInvoker(MethodInfo? methodInfo, RpcPath? path = null,
			Action<RpcServerConfiguration>? configure = null)

		{
			var logger = new XunitLogger<DefaultRpcInvoker>(this._output);
			var options = new Mock<IOptions<RpcServerConfiguration>>(MockBehavior.Strict);
			var matcher = new Mock<IRpcRequestMatcher>(MockBehavior.Strict);
			var accessor = new Mock<IRpcContextAccessor>(MockBehavior.Strict);
			RpcContext requestContext = this.GetRouteContext(path);
			accessor
				.Setup(a => a.Get())
				.Returns(requestContext);
			Moq.Language.Flow.ISetup<IRpcRequestMatcher, IRpcMethodInfo> matcherSetup = matcher
					.Setup(m => m.GetMatchingMethod(It.IsAny<RpcRequestSignature>()));
			if (methodInfo != null)
			{
				//TODO better way of getting this for unit tests?
				DefaultRpcMethodInfo method = DefaultRpcMethodInfo.FromMethodInfo(methodInfo);
				matcherSetup.Returns(method);
			}
			else
			{
				matcherSetup.Throws(new RpcException(RpcErrorCode.MethodNotFound, "Method not found"));
			}
			var config = new RpcServerConfiguration();
			config.ShowServerExceptions = true;
			configure?.Invoke(config);
			options
				.SetupGet(o => o.Value)
				.Returns(config);
			var authHandler = new Mock<IRpcAuthorizationHandler>(MockBehavior.Strict);
			authHandler
				.Setup(h => h.IsAuthorizedAsync(It.IsAny<IRpcMethodInfo>()))
				.Returns(Task.FromResult(true));

			var logger2 = new Mock<ILogger<DefaultRpcParameterConverter>>();
			var parameterConverter = new DefaultRpcParameterConverter(options.Object, logger2.Object);
			return new DefaultRpcInvoker(logger, options.Object, matcher.Object, accessor.Object, authHandler.Object, parameterConverter);
		}

		private IServiceProvider GetServiceProvider()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddScoped<TestInjectionClass>();
			serviceCollection.AddScoped<TestIoCRouteClass>();
			return serviceCollection.BuildServiceProvider();
		}

		private RpcContext GetRouteContext(RpcPath? path = null)
		{
			IServiceProvider serviceProvider = this.GetServiceProvider();
			return new RpcContext(serviceProvider, path);
		}

		[Fact]
		public async Task InvokeRequest_StringParam_ParseAsGuidType()
		{
			Guid randomGuid = Guid.NewGuid();
			var parameters = new  TopLevelRpcParameters(RpcParameter.String(randomGuid.ToString()));
			string methodName = nameof(TestRouteClass.GuidTypeMethod);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);
			RpcResponse? stringResponse = await invoker.InvokeRequestAsync(stringRequest);
			Assert.NotNull(stringResponse);
			Assert.False(stringResponse!.HasError);

			Assert.Equal(randomGuid, stringResponse!.Result);
		}

		[Fact]
		public async Task InvokeRequest_MethodNotFound_ErrorResponse()
		{
			var parameters = new TopLevelRpcParameters(RpcParameter.Number(1));
			var stringRequest = new RpcRequest("1", "MethodNotFound", parameters);
			DefaultRpcInvoker invoker = this.GetInvoker(methodInfo: null);
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			Assert.NotNull(response);
			Assert.NotNull(response!.Error);
			Assert.Equal((int)RpcErrorCode.MethodNotFound, response.Error!.Code);
		}

		[Fact]
		public async Task InvokeRequest_AsyncMethod_Valid()
		{
			var parameters = new  TopLevelRpcParameters(RpcParameter.Number(1), RpcParameter.Number(1));
			string methodName = nameof(TestRouteClass.AddAsync);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(2, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_Int64RequestParam_ConvertToInt32Param()
		{
			var parameters = new  TopLevelRpcParameters(RpcParameter.Number(1L));
			string methodName = nameof(TestRouteClass.IntParameter);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(1, resultResponse.Result);
		}


		[Fact]
		public async Task InvokeRequest_OptionalParameter_Valid()
		{
			string methodName = nameof(TestRouteClass.Optional);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);


			//No params specified
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: null);
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.False(resultResponse.HasError, resultResponse.Error?.Message);

			//Param is empty
			var parameters = new TopLevelRpcParameters();
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.False(resultResponse.HasError);


			//Param is a string
			const string value = "Test";
			parameters = new TopLevelRpcParameters(RpcParameter.String(value));
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.IsType<string>(resultResponse.Result);
			Assert.Equal(value, (string)resultResponse.Result!);
		}

		[Fact]
		public async Task InvokeRequest_DefaultValueParameter_Valid()
		{
			string methodName = nameof(TestRouteClass.DefaultValue);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);


			//No params specified
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: null);
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tezt", resultResponse.Result);
			Assert.False(resultResponse.HasError, resultResponse.Error?.Message);

			//Param is empty
			var parameters = new TopLevelRpcParameters();
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tezt", resultResponse.Result);
			Assert.False(resultResponse.HasError);


			//Param is a string
			const string value = "Test";
			parameters = new TopLevelRpcParameters(RpcParameter.String(value));
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.IsType<string>(resultResponse.Result);
			Assert.Equal(value, (string)resultResponse.Result!);
		}


		[Fact]
		public async Task InvokeRequest_DefaultValue2Parameter_Valid()
		{
			string methodName = nameof(TestRouteClass.DefaultValue2);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);


			//No params specified
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: null);
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.True(resultResponse.HasError, resultResponse.Error?.Message);

			//Param is empty
			var parameters = new TopLevelRpcParameters();
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.True(resultResponse.HasError, resultResponse.Error?.Message);


			// Required param is specified
			const int value = 1;
			parameters = new TopLevelRpcParameters(RpcParameter.Number(value));
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tt", resultResponse.Result);
			Assert.False(resultResponse.HasError);



			// Both are specified
			parameters = new TopLevelRpcParameters(
				RpcParameter.Number(value),
				RpcParameter.String("tets")
			);
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tets", resultResponse.Result);
			Assert.False(resultResponse.HasError);
		}

		[Fact]
		public async Task InvokeRequest_DefaultValue3Parameter_Valid()
		{
			string methodName = nameof(TestRouteClass.DefaultValue3);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);


			//No params specified
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: null);
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tt", resultResponse.Result);
			Assert.False(resultResponse.HasError, resultResponse.Error?.Message);

			//Param is empty
			var parameters = new TopLevelRpcParameters();
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tt", resultResponse.Result);
			Assert.False(resultResponse.HasError, resultResponse.Error?.Message);


			// First param is specified
			const int value = 1;
			parameters = new TopLevelRpcParameters(RpcParameter.Number(value));
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tt", resultResponse.Result);
			Assert.False(resultResponse.HasError);

			// Second param is specified
			const string value2 = "ggg";
			parameters = new TopLevelRpcParameters(new Dictionary<string, RpcParameter>
			{
				["test"] = RpcParameter.String(value2)
			});
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal(value2, resultResponse.Result);
			Assert.False(resultResponse.HasError);



			// Both are specified
			parameters = new TopLevelRpcParameters(
				RpcParameter.Number(value),
				RpcParameter.String("tets")
			);
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Equal("tets", resultResponse.Result);
			Assert.False(resultResponse.HasError);
		}

		[Fact]
		public async Task InvokeRequest_ComplexParam_Valid()
		{
			string methodName = nameof(TestRouteClass.ComplexParam);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			var param = new Dictionary<string, RpcParameter>
			{
				["A"] = RpcParameter.String("Test"),
				["B"] = RpcParameter.Number(5)
			};
			var rpcParameter = RpcParameter.Object(param);
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new  TopLevelRpcParameters(rpcParameter));
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			var obj = Assert.IsType<TestComplexParam>(resultResponse.Result);
			Assert.Equal("Test", obj.A);
			Assert.Equal(5, obj.B);
		}

		[Fact]
		public async Task InvokeRequest_ListParam_Valid()
		{
			string methodName = nameof(TestRouteClass.ListParam);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			var param = new RpcParameter[]
			{
				RpcParameter.Object(new Dictionary<string, RpcParameter>
				{
					["A"] = RpcParameter.String("Test"),
					["B"] = RpcParameter.Number(5)
				}),
				RpcParameter.Object(new Dictionary<string, RpcParameter>
				{
					["A"] = RpcParameter.String("Test2"),
					["B"] = RpcParameter.Number(6)
				}),
			};
			var rpcParameter = RpcParameter.Array(param);
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new  TopLevelRpcParameters(rpcParameter));
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			var obj = Assert.IsType<List<TestComplexParam>>(resultResponse.Result);
			Assert.Equal(2, obj.Count);
			Assert.Equal("Test", obj[0].A);
			Assert.Equal(5, obj[0].B);
			Assert.Equal("Test2", obj[1].A);
			Assert.Equal(6, obj[1].B);
		}

		[Fact]
		public async Task InvokeRequest_BoolParam_Valid()
		{
			string methodName = nameof(TestRouteClass.BoolParameter);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			bool expectedValue = true;
			var param = RpcParameter.Boolean(expectedValue);
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new  TopLevelRpcParameters(param));
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			Assert.Equal(expectedValue, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_BoolDictParam_Valid()
		{
			string methodName = nameof(TestRouteClass.BoolParameter);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			bool expectedValue = true;
			var param = RpcParameter.Boolean(expectedValue);
			var paramDict = new Dictionary<string, RpcParameter>
			{
				["a"] = param
			};
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new  TopLevelRpcParameters(paramDict));
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			Assert.Equal(expectedValue, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_MultipleDictionaryValues_Valid()
		{
			string methodName = nameof(TestRouteClass.AllTypes);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			bool a = true;
			string bb = "Test";
			var dddd = new Dictionary<string, RpcParameter>();
			int eeeee = 1;
			var paramDict = new Dictionary<string, RpcParameter>
			{
				["a"] = RpcParameter.Boolean(a),
				["bb"] = RpcParameter.String(bb),
				["ccc"] = RpcParameter.Null(true),
				["dddd"] = RpcParameter.Object(dddd),
				["eeeee"] = RpcParameter.Number(eeeee)
			};
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new  TopLevelRpcParameters(paramDict));
			RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			(bool, string, object, TestComplexParam, int) value = Assert.IsType<ValueTuple<bool, string, object, TestComplexParam, int>>(resultResponse.Result);
			Assert.Equal(a, value.Item1);
			Assert.Equal(bb, value.Item2);
			Assert.Null(value.Item3);
			Assert.Null(value.Item4.A);
			Assert.Equal(0, value.Item4.B);
			Assert.Equal(eeeee, value.Item5);
		}

		[Fact]
		public async Task InvokeRequest_ComplexParam_TwoRequests_NotCached()
		{
			string methodName = nameof(TestRouteClass.ComplexParam);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			async Task Test(Dictionary<string, RpcParameter> param)
			{
				var rpcParameter = RpcParameter.Object(param);
				RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new  TopLevelRpcParameters(rpcParameter));
				RpcResponse? response = await invoker.InvokeRequestAsync(stringRequest);

				RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
				Assert.False(resultResponse.HasError);
				var obj = Assert.IsType<TestComplexParam>(resultResponse.Result);
				Assert.Equal(param["A"].GetStringValue(), obj.A);
				Assert.True(param["B"].GetNumberValue().TryGetInteger(out int v));
				Assert.Equal(v, obj.B);
			}
			var param1 = new Dictionary<string, RpcParameter>
			{
				["A"] = RpcParameter.String("Test"),
				["B"] = RpcParameter.Number(5)
			};
			await Test(param1);

			var param2 = new Dictionary<string, RpcParameter>
			{
				["A"] = RpcParameter.String("Test2"),
				["B"] = RpcParameter.Number(6)
			};
			await Test(param2);
		}

		[Fact]
		public async Task InvokeRequest_WithAudit()
		{
			Guid randomGuid = Guid.NewGuid();
			var parameters = new  TopLevelRpcParameters(RpcParameter.String(randomGuid.ToString()));
			string methodName = nameof(TestRouteClass.GuidTypeMethod);
			var stringRequest = new RpcRequest("1", methodName, parameters);


			int startCalledCount = 0;
			int endCalledCount = 0;
			object data = new object();

			DefaultRpcInvoker invoker = this.GetInvoker(methodName, configure: (config) =>
			{
				config.OnInvokeStart = (context) =>
				{
					startCalledCount++;
					context.CustomContextData = data;
				};
				config.OnInvokeEnd = (context, response) =>
				{
					endCalledCount++;
					Assert.Same(data, context.CustomContextData);
				};
			});
			RpcResponse? stringResponse = await invoker.InvokeRequestAsync(stringRequest);
			Assert.Equal(1, startCalledCount);
			Assert.Equal(1, endCalledCount);

		}
	}


	public class TestRouteClass
	{
		public Guid GuidTypeMethod(Guid guid)
		{
			return guid;
		}

		public async Task<int> AddAsync(int a, int b)
		{
			return await Task.Run(() => a + b);
		}

		public int IntParameter(int a)
		{
			return a;
		}

		public bool BoolParameter(bool a)
		{
			return a;
		}

		public string? Optional(string? test = null)
		{
			return test;
		}

		public string DefaultValue(string test = "tezt")
		{
			return test;
		}
		public string DefaultValue2(int i, string test = "tt")
		{
			return test;
		}
		public string DefaultValue3(int i = 0, string test = "tt")
		{
			return test;
		}

		public TestComplexParam ComplexParam(TestComplexParam param)
		{
			return param;
		}
		public List<TestComplexParam> ListParam(List<TestComplexParam> param)
		{
			return param;
		}

		public (bool, string, object, TestComplexParam, int) AllTypes(bool a, string bb, object ccc, TestComplexParam dddd, int eeeee)
		{
			return (a, bb, ccc, dddd, eeeee);
		}
	}

	public class TestComplexParam
	{
		public string? A { get; set; }
		public int B { get; set; }

		public override bool Equals(object? obj)
		{
			if (obj is TestComplexParam tcp)
			{
				return this.Equals(tcp);
			}
			return false;
		}
		public bool Equals(TestComplexParam? obj)
		{
			if (obj != null)
			{
				return obj.A == this.A && obj.B == this.B;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return this.A?.GetHashCode() ?? 0 * this.B.GetHashCode();
		}
	}

	public class TestIoCRouteClass
	{
		private TestInjectionClass test { get; }
		public TestIoCRouteClass(TestInjectionClass test)
		{
			this.test = test;
		}

		public int Test()
		{
			return 1;
		}
	}
	public class TestInjectionClass
	{

	}

	public class XunitLogger<T> : ILogger<T>, IDisposable
	{
		private ITestOutputHelper _output;

		public XunitLogger(ITestOutputHelper output)
		{
			this._output = output;
		}
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(formatter(state, exception));
			foreach (var item in this.context)
			{
				if (item is IEnumerable <KeyValuePair<string, object>> col)
				{
					foreach(var c in col)
						sb.AppendLine(c.Key + ": "+ System.Text.Json.JsonSerializer.Serialize(c.Value));
				} else
				{
					sb.AppendLine(System.Text.Json.JsonSerializer.Serialize(item));
				}
			}
			this._output.WriteLine(sb.ToString());
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}
		List<object> context = new();
		public IDisposable? BeginScope<TState>(TState state)
			 where TState : notnull
		{
			this.context.Add(state);
			return new DisposableAction(() => this.context.Remove(state));
		}

		public void Dispose()
		{
		}

		class DisposableAction: IDisposable
		{
			private readonly Action disp;

			public DisposableAction(Action disp)
			{
				this.disp = disp;
			}

			public void Dispose()
			{
				this.disp();
			}
		}
	}
}
