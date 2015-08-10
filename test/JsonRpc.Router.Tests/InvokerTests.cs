using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonRpc.Router.Abstractions;
using Xunit;

namespace JsonRpc.Router.Tests
{
	public class InvokerTests
	{
		private IRpcInvoker SetupInvoker(out RpcRoute route)
		{
			route = new RpcRoute();
			route.AddClass<TestRouteClass>();
			RpcRouteCollection routes = new RpcRouteCollection { route };
			return new DefaultRpcInvoker(routes);
		}

		[Fact]
		public void InvokeRequest_StringParam_ParseAsGuidType()
		{
			Guid randomGuid = Guid.NewGuid();
			RpcRequest stringRequest = new RpcRequest("1", "2.0", "GuidTypeMethod", randomGuid.ToString());
			RpcRoute route;
			IRpcInvoker invoker = this.SetupInvoker(out route);
			RpcResponseBase stringResponse = invoker.InvokeRequest(stringRequest, route);


			RpcResultResponse stringResultResponse = Assert.IsType<RpcResultResponse>(stringResponse);
			Assert.Equal(stringResultResponse.Result, randomGuid);
		}

		[Fact]
		public void InvokeRequest_AmbiguousRequest_ErrorResponse()
		{
			RpcRequest stringRequest = new RpcRequest("1", "2.0", "AmbiguousMethod", 1);
			RpcRoute route;
			IRpcInvoker invoker = this.SetupInvoker(out route);
			RpcResponseBase response = invoker.InvokeRequest(stringRequest, route);

			RpcErrorResponse errorResponse = Assert.IsType<RpcErrorResponse>(response);
			Assert.NotNull(errorResponse.Error);
			Assert.Equal(errorResponse.Error.Code, (int)RpcErrorCode.AmbiguousMethod);
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
	}
}
