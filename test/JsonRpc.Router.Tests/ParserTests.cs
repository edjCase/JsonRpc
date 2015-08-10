using System.Collections.Generic;
using Xunit;

namespace JsonRpc.Router.Tests
{
	public class ParserTests
	{
		[Theory]
		[InlineData("/", null, true)]
		[InlineData("/", "", true)]
		[InlineData("/", "Test", false)]
		[InlineData("/Test", "Test", true)]
		[InlineData("/Test/Test2", "Test", false)]
		[InlineData("/Test/Test2", "Test/Test2", true)]
		[InlineData("Test", "Test", true)]
		public void MatchesRpcRoute_DifferentRoutes_Valid(string requestUrl, string availableRouteName, bool shouldMatch)
		{
			RpcRoute route = new RpcRoute(availableRouteName);
			RpcRouteCollection routes = new RpcRouteCollection {route};

			DefaultRpcParser parser = new DefaultRpcParser(null, routes);
			RpcRoute matchedRoute;
			bool isMatch = parser.MatchesRpcRoute(requestUrl, out matchedRoute);
			Assert.Equal(isMatch, shouldMatch);
			Assert.Equal(matchedRoute != null, shouldMatch);
			Assert.Equal(route == matchedRoute, shouldMatch);
		}
	}
}
