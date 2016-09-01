using System;
using System.Collections.Generic;
using System.Linq;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Defaults;
using Xunit;
using EdjCase.JsonRpc.Router.Abstractions;

namespace EdjCase.JsonRpc.Router.Tests
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
			IRpcRouteProvider routeProvider = new FakeRouteProvider();
			RouteCriteria routeCriteria = new RouteCriteria(typeof(ParserTests));
			routeProvider.RegisterRoute(routeCriteria, availableRouteName);
			DefaultRpcParser parser = new DefaultRpcParser(null);
			RpcRoute matchedRoute;
			bool isMatch = parser.MatchesRpcRoute(routeProvider, requestUrl, out matchedRoute);
			Assert.Equal(isMatch, shouldMatch);
			Assert.Equal(matchedRoute != null, shouldMatch);
		}

		[Theory]
		[InlineData("{\"jsonrpc\": \"2.0\", \"method\": \"subtract\", \"params\": [42, 23], \"id\": 1}", (long)1, "subtract", new object[] { (long)42, (long)23 })]
		[InlineData("{\"jsonrpc\": \"2.0\", \"method\": \"subtract2\", \"params\": [\"42\", \"23\"], \"id\": \"4\"}", "4", "subtract2", new object[] { "42", "23" })]
		public void ParseRequests_Valid(string json, object id, string method, object[] parameters)
		{
			DefaultRpcParser parser = new DefaultRpcParser(null);

			RpcRequest rpcRequest = parser.ParseRequests(json).FirstOrDefault();

			Assert.NotNull(rpcRequest);
			Assert.Equal(rpcRequest.Id, id);
			Assert.Equal(rpcRequest.Method, method);
			Assert.Equal(rpcRequest.JsonRpcVersion, JsonRpcContants.JsonRpcVersion);
			Assert.Equal(rpcRequest.ParameterList, parameters);
		}

		[Fact]
		public void ParseRequests_DateTime_Valid()
		{
			const string json = "{\"jsonrpc\": \"2.0\", \"method\": \"datetime\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": 1}";
			DateTime dateTime = DateTime.Parse("2000-12-15T22:11:03");
			DefaultRpcParser parser = new DefaultRpcParser(null);

			RpcRequest rpcRequest = parser.ParseRequests(json).FirstOrDefault();

			Assert.NotNull(rpcRequest);
			Assert.Equal(rpcRequest.Id, (long)1);
			Assert.Equal(rpcRequest.Method, "datetime");
			Assert.Equal(rpcRequest.JsonRpcVersion, JsonRpcContants.JsonRpcVersion);
			Assert.Equal(rpcRequest.ParameterList, new object[] { dateTime });
		}

		[Fact]
		public void ParseRequests_BatchRequest_Valid()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}, {\"jsonrpc\": \"2.0\", \"method\": \"two\", \"params\": [\"2\"], \"id\": \"2\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(null);

			List<RpcRequest> rpcRequests = parser.ParseRequests(json);

			Assert.NotNull(rpcRequests);
			Assert.Equal(rpcRequests.Count, 2);
			Assert.Equal(rpcRequests[0].Id, "1");
			Assert.Equal(rpcRequests[0].Method, "one");
			Assert.Equal(rpcRequests[0].JsonRpcVersion, JsonRpcContants.JsonRpcVersion);
			Assert.Equal(rpcRequests[0].ParameterList, new object[] { "1" });

			Assert.Equal(rpcRequests[1].Id, "2");
			Assert.Equal(rpcRequests[1].Method, "two");
			Assert.Equal(rpcRequests[1].JsonRpcVersion, JsonRpcContants.JsonRpcVersion);
			Assert.Equal(rpcRequests[1].ParameterList, new object[] { "2" });
		}

		[Fact]
		public void ParseRequests_NullRequest_InvalidRequestException()
		{
			const string json = null;
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json));
		}

		[Fact]
		public void ParseRequests_EmptyObjectRequest_InvalidRequestException()
		{
			const string json = "{}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json));
		}

		[Fact]
		public void ParseRequests_MissingVersion_InvalidRequestException()
		{
			const string json = "{\"method\": \"datetime\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json));
		}

		[Fact]
		public void ParseRequests_MissingMethod_InvalidRequestException()
		{
			const string json = "{\"jsonrpc\": \"2.0\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json));
		}

		[Fact]
		public void ParseRequests_MissingId_NoException()
		{
			const string json = "{\"method\": \"datetime\", \"jsonrpc\": \"2.0\", \"params\": [\"2000-12-15T22:11:03\"]}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			parser.ParseRequests(json);
		}

		[Fact]
		public void ParseRequests_MissingParams_NoException()
		{
			const string json = "{\"method\": \"datetime\",\"jsonrpc\": \"2.0\", \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			parser.ParseRequests(json);
		}
	}

	public class FakeRouteProvider : IRpcRouteProvider
	{
		public bool AutoDetectControllers { get; set; }
		private List<RpcRoute> routes { get; } = new List<RpcRoute>();

		public List<RpcRoute> GetRoutes()
		{
			return routes;
		}

		public void RegisterRoute(IEnumerable<RouteCriteria> criteria, string name = null)
		{
			this.routes.Add(new RpcRoute(criteria.ToList(), name));
		}
	}
}
