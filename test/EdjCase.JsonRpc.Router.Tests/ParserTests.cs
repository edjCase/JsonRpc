using System;
using System.Collections.Generic;
using System.Linq;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Defaults;
using Xunit;
using Newtonsoft.Json.Linq;

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
		public void ParsePath_DifferentPaths_Valid(string requestUrl, string availableRouteName, bool shouldMatch)
		{
			RpcPath requestPath = RpcPath.Parse(requestUrl);
			RpcPath routePath = RpcPath.Parse(availableRouteName);

			if (shouldMatch)
			{
				Assert.Equal(routePath, requestPath);
			}
			else
			{
				Assert.NotEqual(routePath, requestPath);
			}
		}

		[Fact]
		public void AddPath_Same_Match()
		{
			RpcPath fullPath = RpcPath.Parse("/Base/Test");
			RpcPath otherPath = RpcPath.Parse("/Base").Add(RpcPath.Parse("/Test"));
			Assert.Equal(fullPath, otherPath);
		}


		[Fact]
		public void AddPath_Different_NoMatch()
		{
			RpcPath fullPath = RpcPath.Parse("/Base/Test");
			RpcPath otherPath = fullPath.Add(RpcPath.Parse("/Test"));
			Assert.NotEqual(fullPath, otherPath);
		}

		[Theory]
		[InlineData("{\"jsonrpc\": \"2.0\", \"method\": \"subtract\", \"params\": [42, 23], \"id\": 1}", 1, "subtract", new object[] { 42, 23 })]
		[InlineData("{\"jsonrpc\": \"2.0\", \"method\": \"subtract2\", \"params\": [\"42\", \"23\"], \"id\": \"4\"}", "4", "subtract2", new object[] { "42", "23" })]
		public void ParseRequests_Valid(string json, object id, string method, object[] parameters)
		{
			DefaultRpcParser parser = new DefaultRpcParser(null);

			RpcRequest rpcRequest = parser.ParseRequests(json, out bool isBulkRequest).FirstOrDefault();

			Assert.NotNull(rpcRequest);
			ParserTests.CompareId(id, rpcRequest.Id);
			Assert.Equal(method, rpcRequest.Method);
			Assert.Equal(JsonRpcContants.JsonRpcVersion, rpcRequest.JsonRpcVersion);
			ParserTests.CompareParameters(parameters, rpcRequest.Parameters);
			Assert.False(isBulkRequest);
		}

		private static void CompareId(object id, RpcId jId)
		{
			if (!jId.HasValue)
			{
				Assert.Null(id);
				return;
			}
			if (jId.IsString)
			{
				Assert.Equal(id, jId.StringValue);
				return;
			}
			id = Convert.ToDouble(id);
			Assert.Equal(id, jId.NumberValue);
		}

		private static void CompareParameters(object[] parameters, JToken jParameters)
		{
			if (parameters != null)
			{
				Assert.NotNull(jParameters);
				Assert.Equal(JTokenType.Array, jParameters.Type);
				JToken[] jArray = jParameters.ToArray();
				Assert.Equal(parameters.Length, jArray.Length);
				//TODO compare types?
			}
			else
			{
				Assert.Null(jParameters);
			}
		}

		[Fact]
		public void ParseRequests_DateTime_Valid()
		{
			const string json = "{\"jsonrpc\": \"2.0\", \"method\": \"datetime\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": 1}";
			DateTime dateTime = DateTime.Parse("2000-12-15T22:11:03");
			DefaultRpcParser parser = new DefaultRpcParser(null);

			RpcRequest rpcRequest = parser.ParseRequests(json, out bool isBulkRequest).FirstOrDefault();

			Assert.NotNull(rpcRequest);
			ParserTests.CompareId(1, rpcRequest.Id);
			Assert.Equal("datetime", rpcRequest.Method);
			Assert.Equal(JsonRpcContants.JsonRpcVersion, rpcRequest.JsonRpcVersion);
			ParserTests.CompareParameters(new object[] { dateTime }, rpcRequest.Parameters);
			Assert.False(isBulkRequest);
		}

		[Fact]
		public void ParseRequests_BatchRequest_Valid()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}, {\"jsonrpc\": \"2.0\", \"method\": \"two\", \"params\": [\"2\"], \"id\": \"2\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(null);

			List<RpcRequest> rpcRequests = parser.ParseRequests(json, out bool isBulkRequest);

			Assert.NotNull(rpcRequests);
			Assert.Equal(2, rpcRequests.Count);
			ParserTests.CompareId("1", rpcRequests[0].Id);
			Assert.Equal("one", rpcRequests[0].Method);
			Assert.Equal(JsonRpcContants.JsonRpcVersion, rpcRequests[0].JsonRpcVersion);
			ParserTests.CompareParameters(new object[] { "1" }, rpcRequests[0].Parameters);

			ParserTests.CompareId("2", rpcRequests[1].Id);
			Assert.Equal("two", rpcRequests[1].Method);
			Assert.Equal(JsonRpcContants.JsonRpcVersion, rpcRequests[1].JsonRpcVersion);
			ParserTests.CompareParameters(new object[] { "2" }, rpcRequests[1].Parameters);
			Assert.True(isBulkRequest);
		}

		[Fact]
		public void ParseRequests_DuplicateIds_InvalidRequestException()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}, {\"jsonrpc\": \"2.0\", \"method\": \"two\", \"params\": [\"2\"], \"id\": \"1\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.ThrowsAny<RpcInvalidRequestException>(() => parser.ParseRequests(json, out bool isBulkRequest));
			
		}

		[Fact]
		public void ParseRequests_SingleBatchRequest_Valid()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(null);

			List<RpcRequest> rpcRequests = parser.ParseRequests(json, out bool isBulkRequest);

			Assert.NotNull(rpcRequests);
			Assert.Single(rpcRequests);
			ParserTests.CompareId("1", rpcRequests[0].Id);
			Assert.Equal("one", rpcRequests[0].Method);
			Assert.Equal(JsonRpcContants.JsonRpcVersion, rpcRequests[0].JsonRpcVersion);
			ParserTests.CompareParameters(new object[] { "1" }, rpcRequests[0].Parameters);
			Assert.True(isBulkRequest);
		}

		[Fact]
		public void ParseRequests_NullRequest_InvalidRequestException()
		{
			const string json = null;
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json, out bool isBulkRequest));
		}

		[Fact]
		public void ParseRequests_EmptyObjectRequest_InvalidRequestException()
		{
			const string json = "{}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json, out bool isBulkRequest));
		}

		[Fact]
		public void ParseRequests_MissingVersion_InvalidRequestException()
		{
			const string json = "{\"method\": \"datetime\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json, out bool isBulkRequest));
		}

		[Fact]
		public void ParseRequests_MissingMethod_InvalidRequestException()
		{
			const string json = "{\"jsonrpc\": \"2.0\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			Assert.Throws<RpcInvalidRequestException>(() => parser.ParseRequests(json, out bool isBulkRequest));
		}

		[Fact]
		public void ParseRequests_MissingId_NoException()
		{
			const string json = "{\"method\": \"datetime\", \"jsonrpc\": \"2.0\", \"params\": [\"2000-12-15T22:11:03\"]}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			parser.ParseRequests(json, out bool isBulkRequest);
			Assert.False(isBulkRequest);
		}

		[Fact]
		public void ParseRequests_MissingParams_NoException()
		{
			const string json = "{\"method\": \"datetime\",\"jsonrpc\": \"2.0\", \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(null);

			parser.ParseRequests(json, out bool isBulkRequest);
			Assert.False(isBulkRequest);
		}
	}
}
