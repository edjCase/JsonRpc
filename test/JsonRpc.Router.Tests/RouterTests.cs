using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Xunit;

namespace JsonRpc.Router.Tests
{
	public class RouterTests
	{
		[Fact]
		public void Test1()
		{
			//IRpcInvoker invoker = new FakeInvoker();
			//RpcRouter router = new RpcRouter(configuration, invoker);


			//await router.RouteAsync(routeContext);
		}
	}

	public class FakeInvoker : IRpcInvoker
	{
		public RpcResponseBase InvokeRequest(RpcRequest request, string section)
		{
			return new RpcResultResponse(request.Id, "Fake Result");
		}
	}
}
