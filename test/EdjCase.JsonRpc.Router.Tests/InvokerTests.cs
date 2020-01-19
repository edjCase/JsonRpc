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

namespace EdjCase.JsonRpc.Router.Tests
{
	public class InvokerTests
	{
		private DefaultRpcInvoker GetInvoker(string? methodName, RpcPath? path = null)
		{
			var authorizationService = new Mock<IAuthorizationService>();
			var policyProvider = new Mock<IAuthorizationPolicyProvider>();
			var logger = new Mock<ILogger<DefaultRpcInvoker>>();
			var options = new Mock<IOptions<RpcServerConfiguration>>();
			var matcher = new Mock<IRpcRequestMatcher>();
			var accessor = new Mock<IRpcContextAccessor>();
			IRpcContext requestContext = this.GetRouteContext(path);
			accessor
				.SetupGet(a => a.Value)
				.Returns(requestContext);
			if (methodName != null)
			{
				MethodInfo methodInfo = typeof(TestRouteClass).GetMethod(methodName)!;
				//TODO better way of getting this for unit tests?
				RpcMethodInfo method = DefaultRequestMatcher.BuildMethodInfo(methodInfo);
				matcher
					.Setup(m => m.GetMatchingMethod(It.IsAny<RpcRequestSignature>()))
					.Returns(method);
			}
			var config = new RpcServerConfiguration();
			config.ShowServerExceptions = true;
			options
				.SetupGet(o => o.Value)
				.Returns(config);

			return new DefaultRpcInvoker(authorizationService.Object, policyProvider.Object, logger.Object, options.Object, matcher.Object, accessor.Object);
		}

		private IServiceProvider GetServiceProvider()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddScoped<TestInjectionClass>();
			serviceCollection.AddScoped<TestIoCRouteClass>();
			return serviceCollection.BuildServiceProvider();
		}

		private IRpcContext GetRouteContext(RpcPath? path = null)
		{
			IServiceProvider serviceProvider = this.GetServiceProvider();
			var routeContext = new Mock<IRpcContext>(MockBehavior.Strict);
			routeContext
				.SetupGet(rc => rc.Path)
				.Returns(path);
			routeContext
				.SetupGet(rc => rc.RequestServices)
				.Returns(serviceProvider);
			routeContext
				.SetupGet(rc => rc.User)
				.Returns(new System.Security.Claims.ClaimsPrincipal());
			return routeContext.Object;
		}

		[Fact]
		public async Task InvokeRequest_StringParam_ParseAsGuidType()
		{
			Guid randomGuid = Guid.NewGuid();
			var parameters = new RpcParameters(new RawRpcParameter(RpcParameterType.String, randomGuid.ToString()));
			string methodName = nameof(TestRouteClass.GuidTypeMethod);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);
			RpcResponse stringResponse = await invoker.InvokeRequestAsync(stringRequest);


			Assert.Equal(randomGuid, stringResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_AmbiguousRequest_ErrorResponse()
		{
			var parameters = new RpcParameters(new RawRpcParameter(RpcParameterType.Number, 1));
			string methodName = nameof(TestRouteClass.AmbiguousMethod);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			Assert.NotNull(response.Error);
			Assert.Equal((int)RpcErrorCode.MethodNotFound, response.Error.Code);
		}

		[Fact]
		public async Task InvokeRequest_AsyncMethod_Valid()
		{
			var parameters = new RpcParameters(new RawRpcParameter(RpcParameterType.Number, 1), new RawRpcParameter(RpcParameterType.Number, 1));
			string methodName = nameof(TestRouteClass.AddAsync);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(2, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_Int64RequestParam_ConvertToInt32Param()
		{
			var parameters = new RpcParameters(new RawRpcParameter(RpcParameterType.Number, 1L));
			string methodName = nameof(TestRouteClass.IntParameter);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(1, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_ServiceProvider_Pass()
		{
			var stringRequest = new RpcRequest("1", "Test");

			DefaultRpcInvoker invoker = this.GetInvoker(methodName: null);
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

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
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.False(resultResponse.HasError, resultResponse.Error?.Message);

			//Param is empty
			var parameters = new RpcParameters(new IRpcParameter[0]);
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.False(resultResponse.HasError);


			//Param is a string
			const string value = "Test";
			parameters = new RpcParameters(new IRpcParameter[] { new RawRpcParameter(RpcParameterType.String, value) });
			stringRequest = new RpcRequest("1", methodName, parameters: parameters);
			response = await invoker.InvokeRequestAsync(stringRequest);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.IsType<string>(resultResponse.Result);
			Assert.Equal(value, (string)resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_ComplexParam_Valid()
		{
			string methodName = nameof(TestRouteClass.ComplexParam);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			TestComplexParam param = new TestComplexParam
			{
				A = "Test",
				B = 5
			};
			var rpcParameter = new RawRpcParameter(RpcParameterType.Object, param);
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new RpcParameters(rpcParameter));
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			Assert.Same(param, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_BoolParam_Valid()
		{
			string methodName = nameof(TestRouteClass.BoolParameter);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			var param = new RawRpcParameter(RpcParameterType.Boolean, true);
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new RpcParameters(param));
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			Assert.Equal(param.Value, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_BoolDictParam_Valid()
		{
			string methodName = nameof(TestRouteClass.BoolParameter);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			var param = new RawRpcParameter(RpcParameterType.Boolean, true);
			var paramDict = new Dictionary<string, IRpcParameter>
			{
				["a"] = param
			};
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new RpcParameters(paramDict));
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			Assert.Equal(param.Value, resultResponse.Result);
		}

		[Fact]
		public async Task InvokeRequest_MultipleDictionaryValues_Valid()
		{
			string methodName = nameof(TestRouteClass.AllTypes);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			bool a = true;
			string bb = "Test";
			object? ccc = null;
			var dddd = new TestComplexParam();
			int eeeee = 1;
			var paramDict = new Dictionary<string, IRpcParameter>
			{
				["a"] = new RawRpcParameter(RpcParameterType.Boolean, a),
				["bb"] = new RawRpcParameter(RpcParameterType.String, bb),
				["ccc"] = new RawRpcParameter(RpcParameterType.Null, ccc),
				["dddd"] = new RawRpcParameter(RpcParameterType.Object, dddd),
				["eeeee"] = new RawRpcParameter(RpcParameterType.Number, eeeee)
			};
			RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new RpcParameters(paramDict));
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.False(resultResponse.HasError);
			var value = Assert.IsType<ValueTuple<bool, string, object, object, int>>(resultResponse.Result);
			Assert.Equal((a, bb, ccc, dddd, eeeee), value);
		}

		[Fact]
		public async Task InvokeRequest_ComplexParam_TwoRequests_NotCached()
		{
			string methodName = nameof(TestRouteClass.ComplexParam);
			DefaultRpcInvoker invoker = this.GetInvoker(methodName);

			async Task Test(TestComplexParam param)
			{
				var rpcParameter = new RawRpcParameter(RpcParameterType.Object, param);
				RpcRequest stringRequest = new RpcRequest("1", methodName, parameters: new RpcParameters(rpcParameter));
				RpcResponse response = await invoker.InvokeRequestAsync(stringRequest);

				RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
				Assert.False(resultResponse.HasError);
				Assert.Same(param, resultResponse.Result);
			}
			TestComplexParam param1 = new TestComplexParam
			{
				A = "Test",
				B = 5
			};
			await Test(param1);
			TestComplexParam param2 = new TestComplexParam
			{
				A = "Test2",
				B = 6
			};
			await Test(param2);
		}
	}


	public class TestRouteClass
	{
		public Guid GuidTypeMethod(Guid guid)
		{
			return guid;
		}

		public int AmbiguousMethod(int a)
		{
			return a;
		}

		public long AmbiguousMethod(long a)
		{
			return a;
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

		public TestComplexParam ComplexParam(TestComplexParam param)
		{
			return param;
		}

		public (bool, string, object, object, int) AllTypes(bool a, string bb, object ccc, object dddd, int eeeee)
		{
			return (a, bb, ccc, dddd, eeeee);
		}
	}

	public class TestComplexParam
	{
		public string? A { get; set; }
		public int B { get; set; }
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
}
