using System;
using System.Threading.Tasks;
using edjCase.JsonRpc.Router.Abstractions;
using edjCase.JsonRpc.Router.Defaults;
using Xunit;

namespace edjCase.JsonRpc.Router.Tests
{
	public class InvokerTests
	{
		[Fact]
		public void InvokeRequest_StringParam_ParseAsGuidType()
		{
			Guid randomGuid = Guid.NewGuid();
			RpcRequest stringRequest = new RpcRequest("1", "2.0", "GuidTypeMethod", randomGuid.ToString());

			RpcRoute route = new RpcRoute();
			route.AddClass<TestRouteClass>();
			IRpcInvoker invoker = new DefaultRpcInvoker();
			RpcResponseBase stringResponse = invoker.InvokeRequest(stringRequest, route);


			RpcResultResponse stringResultResponse = Assert.IsType<RpcResultResponse>(stringResponse);
			Assert.Equal(stringResultResponse.Result, randomGuid);
		}

		[Fact]
		public void InvokeRequest_AmbiguousRequest_ErrorResponse()
		{
			RpcRequest stringRequest = new RpcRequest("1", "2.0", "AmbiguousMethod", 1);
			RpcRoute route = new RpcRoute();
			route.AddClass<TestRouteClass>();
			IRpcInvoker invoker = new DefaultRpcInvoker();
			RpcResponseBase response = invoker.InvokeRequest(stringRequest, route);

			RpcErrorResponse errorResponse = Assert.IsType<RpcErrorResponse>(response);
			Assert.NotNull(errorResponse.Error);
			Assert.Equal(errorResponse.Error.Code, (int)RpcErrorCode.AmbiguousMethod);
		}

		[Fact]
		public void InvokeRequest_AsyncMethod_Valid()
		{
			RpcRequest stringRequest = new RpcRequest("1", "2.0", "AddAsync", 1, 1);
			RpcRoute route = new RpcRoute();
			route.AddClass<TestRouteClass>();
			IRpcInvoker invoker = new DefaultRpcInvoker();

			RpcResponseBase response = invoker.InvokeRequest(stringRequest, route);

			RpcResultResponse resultResponse = Assert.IsType<RpcResultResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.Equal(resultResponse.Result, 2);
		}

		[Fact]
		public void InvokeRequest_Int64RequestParam_ConvertToInt32Param()
		{
			RpcRequest stringRequest = new RpcRequest("1", "2.0", "IntParameter", (long)1);
			RpcRoute route = new RpcRoute();
			route.AddClass<TestRouteClass>();
			IRpcInvoker invoker = new DefaultRpcInvoker();

			RpcResponseBase response = invoker.InvokeRequest(stringRequest, route);

			RpcResultResponse resultResponse = Assert.IsType<RpcResultResponse>(response);
			Assert.NotNull(resultResponse.Result);
			Assert.IsType<int>(resultResponse.Result);
			Assert.Equal(resultResponse.Result, 1);
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
	}
}
