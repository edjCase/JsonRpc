﻿using System;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
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

namespace EdjCase.JsonRpc.Router.Tests
{
	public class InvokerTests
	{
		private DefaultRpcInvoker GetInvoker()
		{
			var authorizationService = new Mock<IAuthorizationService>();
			var policyProvider = new Mock<IAuthorizationPolicyProvider>();
			var logger = new Mock<ILogger<DefaultRpcInvoker>>();
			var options = new Mock<IOptions<RpcServerConfiguration>>();
			var config = new RpcServerConfiguration();
			config.ShowServerExceptions = true;
			options
				.SetupGet(o => o.Value)
				.Returns(config);

			return new DefaultRpcInvoker(authorizationService.Object, policyProvider.Object, logger.Object, options.Object);
		}

		private IServiceProvider GetServiceProvider()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddScoped<TestInjectionClass>();
			serviceCollection.AddScoped<TestIoCRouteClass>();
			return serviceCollection.BuildServiceProvider();
		}

		private IRouteContext GetRouteContext()
		{
			IServiceProvider serviceProvider = this.GetServiceProvider();
			var routeContext = new Mock<IRouteContext>(MockBehavior.Strict);
			routeContext
				.SetupGet(rc => rc.RequestServices)
				.Returns(serviceProvider);
			routeContext
				.SetupGet(rc => rc.User)
				.Returns(new System.Security.Claims.ClaimsPrincipal());
			return routeContext.Object;
		}

		//TODO httpcontext mock?
		[Fact]
		public async Task InvokeRequest_StringParam_ParseAsGuidType()
		{
			Guid randomGuid = Guid.NewGuid();
			RpcRequest stringRequest = new RpcRequest("1", "GuidTypeMethod", randomGuid.ToString());

			var routeCriteria = new List<RouteCriteria>();
			routeCriteria.Add(new RouteCriteria(typeof(TestRouteClass)));
			RpcRoute route = new RpcRoute(routeCriteria);

			IRouteContext routeContext = this.GetRouteContext();
			DefaultRpcInvoker invoker = this.GetInvoker();
			RpcResponse stringResponse = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);


			Assert.Equal(stringResponse.Result, randomGuid);
		}

		[Fact]
		public async Task InvokeRequest_AmbiguousRequest_ErrorResponse()
		{
			RpcRequest stringRequest = new RpcRequest("1", "AmbiguousMethod", 1);


			var routeCriteria = new List<RouteCriteria>();
			routeCriteria.Add(new RouteCriteria(typeof(TestRouteClass)));
			RpcRoute route = new RpcRoute(routeCriteria);
			IRouteContext routeContext = this.GetRouteContext();
			DefaultRpcInvoker invoker = this.GetInvoker();
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			Assert.NotNull(response.Error);
			Assert.Equal(response.Error.Code, (int)RpcErrorCode.MethodNotFound);
		}

		[Fact]
		public async Task InvokeRequest_AsyncMethod_Valid()
		{
			RpcRequest stringRequest = new RpcRequest("1", "AddAsync", 1, 1);


			var routeCriteria = new List<RouteCriteria>();
			routeCriteria.Add(new RouteCriteria(typeof(TestRouteClass)));
			RpcRoute route = new RpcRoute(routeCriteria);

			IRouteContext routeContext = this.GetRouteContext();
			DefaultRpcInvoker invoker = this.GetInvoker();

			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(resultResponse.Result, 2);
		}

		[Fact]
		public async Task InvokeRequest_Int64RequestParam_ConvertToInt32Param()
		{
			RpcRequest stringRequest = new RpcRequest("1", "IntParameter", (long)1);


			var routeCriteria = new List<RouteCriteria>();
			routeCriteria.Add(new RouteCriteria(typeof(TestRouteClass)));
			RpcRoute route = new RpcRoute(routeCriteria);
			IRouteContext routeContext = this.GetRouteContext();
			DefaultRpcInvoker invoker = this.GetInvoker();

			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(JTokenType.Integer, resultResponse.Result.Type);
			Assert.Equal(resultResponse.Result.Value<int>(), 1);
		}

		[Fact]
		public async Task InvokeRequest_ServiceProvider_Pass()
		{
			RpcRequest stringRequest = new RpcRequest("1", "Test");
			var routeCriteria = new List<RouteCriteria>();
			routeCriteria.Add(new RouteCriteria(typeof(TestIoCRouteClass)));
			RpcRoute route = new RpcRoute(routeCriteria);

			DefaultRpcInvoker invoker = this.GetInvoker();
			IServiceProvider serviceProvider = this.GetServiceProvider();
			IRouteContext routeContext = this.GetRouteContext();
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(JTokenType.Integer, resultResponse.Result.Type);
			Assert.Equal(resultResponse.Result.Value<int>(), 1);
		}

		[Fact]
		public async Task InvokeRequest_OptionalParameter_Valid()
		{
			var routeCriteria = new List<RouteCriteria>();
			routeCriteria.Add(new RouteCriteria(typeof(TestRouteClass)));
			RpcRoute route = new RpcRoute(routeCriteria);

			DefaultRpcInvoker invoker = this.GetInvoker();
			IServiceProvider serviceProvider = this.GetServiceProvider();
			IRouteContext routeContext = this.GetRouteContext();


			//No params specified
			RpcRequest stringRequest = new RpcRequest("1", "Optional");
			RpcResponse response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			RpcResponse resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.False(resultResponse.HasError);

			//Param is null
			stringRequest = new RpcRequest("1", "Optional", parameterList: null);
			response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.Null(resultResponse.Result);
			Assert.False(resultResponse.HasError);


			//Param is a string
			stringRequest = new RpcRequest("1", "Optional", parameterList: "Test");
			response = await invoker.InvokeRequestAsync(stringRequest, route, routeContext);

			resultResponse = Assert.IsType<RpcResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(JTokenType.String, resultResponse.Result.Type);
			Assert.Equal(resultResponse.Result.Value<string>(), "Test");
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

		public string Optional(string test = null)
		{
			return test;
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
}
