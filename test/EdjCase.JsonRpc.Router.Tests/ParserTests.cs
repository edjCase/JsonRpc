using System;
using System.Collections.Generic;
using System.Linq;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Defaults;
using Xunit;
using Newtonsoft.Json.Linq;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

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
		[InlineData("Test", "test", true)]
		[InlineData("test", "Test", true)]
		[InlineData("test/Test", "Test/test", true)]
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

		[Theory]
		[InlineData("/", null)]
		[InlineData("/Test", "test")]
		[InlineData("/Test/", "test")]
		[InlineData("Test/", "test")]
		[InlineData("/Test/Test2", "test/test2")]
		[InlineData("test", "test")]
		public void ToString_LowerCaseAndTrimSlash(string path, string expected)
		{
			RpcPath fullPath = RpcPath.Parse(path);
			Assert.Equal(expected, fullPath?.ToString());
		}

		[Fact]
		public void AddPath_Same_Match()
		{
			RpcPath fullPath = RpcPath.Parse("/Base/Test");
			RpcPath otherPath = RpcPath.Parse("/Base").Add(RpcPath.Parse("/Test"));
			Assert.Equal(fullPath, otherPath);
		}

		[Fact]
		public void TryRemoveBasePath_2Part_1PartOutput()
		{
			RpcPath basePath = RpcPath.Parse("/Base");
			RpcPath fullPath = RpcPath.Parse("/Base/Test");
			bool removed = fullPath.TryRemoveBasePath(basePath, out RpcPath path);
			Assert.True(removed);
			Assert.Equal(RpcPath.Parse("Test"), path);

			//Also check the Remove is the same
			RpcPath path2 = fullPath.RemoveBasePath(basePath);
			Assert.Equal(path, path2);
		}

		[Fact]
		public void TryRemoveBasePath_1Part_NullOutput()
		{
			RpcPath basePath = RpcPath.Parse("/Base");
			RpcPath fullPath = RpcPath.Parse("/Base/");
			bool removed = fullPath.TryRemoveBasePath(basePath, out RpcPath path);
			Assert.True(removed);
			Assert.Null(path);

			//Also check the Remove is the same
			RpcPath path2 = fullPath.RemoveBasePath(basePath);
			Assert.Equal(path, path2);
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
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Requests);
			Assert.Equal(RpcId.FromObject(id), result.Requests[0].Id);
			Assert.Equal(method, result.Requests[0].Method);
			ParserTests.CompareParameters(parameters, result.Requests[0].Parameters);
			Assert.False(result.IsBulkRequest);
		}

		private static void CompareParameters(object[] parameters, RpcParameters? jParameters)
		{
			if (parameters != null)
			{
				Assert.NotNull(jParameters);
				Assert.False(jParameters!.IsDictionary);
				Assert.Equal(parameters.Length, jParameters.AsArray.Length);
				//TODO compare types?
			}
			else
			{
				Assert.Equal(default(RpcParameters), jParameters);
			}
		}

		[Fact]
		public void ParseRequests_DateTime_Valid()
		{
			const string json = "{\"jsonrpc\": \"2.0\", \"method\": \"datetime\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": 1}";
			DateTime dateTime = DateTime.Parse("2000-12-15T22:11:03");
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);


			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Requests);
			Assert.Equal(1, result.Requests[0].Id);
			Assert.Equal("datetime", result.Requests[0].Method);
			ParserTests.CompareParameters(new object[] { dateTime }, result.Requests[0].Parameters);
			Assert.False(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_BatchRequest_Valid()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}, {\"jsonrpc\": \"2.0\", \"method\": \"two\", \"params\": [\"2\"], \"id\": \"2\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);

			Assert.Equal(2, result.RequestCount);
			Assert.Equal(2, result.Requests.Count);
			Assert.Equal("1", result.Requests[0].Id);
			Assert.Equal("one", result.Requests[0].Method);
			ParserTests.CompareParameters(new object[] { "1" }, result.Requests[0].Parameters);

			Assert.Equal("2", result.Requests[1].Id);
			Assert.Equal("two", result.Requests[1].Method);

			ParserTests.CompareParameters(new object[] { "2" }, result.Requests[1].Parameters);

			Assert.True(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_DuplicateIds_InvalidRequestException()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}, {\"jsonrpc\": \"2.0\", \"method\": \"two\", \"params\": [\"2\"], \"id\": \"1\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			var ex = Assert.Throws<RpcException>(() => parser.ParseRequests(json));
			Assert.Equal((int)RpcErrorCode.InvalidRequest, ex.ErrorCode);

		}

		[Fact]
		public void ParseRequests_SingleBatchRequest_Valid()
		{
			const string json = "[{\"jsonrpc\": \"2.0\", \"method\": \"one\", \"params\": [\"1\"], \"id\": \"1\"}]";

			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Requests);
			Assert.Equal("1", result.Requests[0].Id);
			Assert.Equal("one", result.Requests[0].Method);
			ParserTests.CompareParameters(new object[] { "1" }, result.Requests[0].Parameters);
			Assert.True(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_NullRequest_InvalidRequestException()
		{
			const string? json = null;
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			var ex = Assert.Throws<RpcException>(() => parser.ParseRequests(json));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			Assert.Equal((int)RpcErrorCode.InvalidRequest, ex.ErrorCode);
		}

		[Fact]
		public void ParseRequests_EmptyObjectRequest_InvalidRequestException()
		{
			const string json = "{}";
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Errors);
			Assert.Equal(default(RpcId), result.Errors[0].Id);
			Assert.NotNull(result.Errors[0].Error);
			Assert.Equal((int)RpcErrorCode.InvalidRequest, result.Errors[0].Error.Code);
			Assert.False(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_MissingVersion_InvalidRequestException()
		{
			const string json = "{\"method\": \"datetime\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Errors);
			Assert.Equal("1", result.Errors[0].Id);
			Assert.NotNull(result.Errors[0].Error);
			Assert.Equal((int)RpcErrorCode.InvalidRequest, result.Errors[0].Error.Code);
			Assert.False(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_MissingMethod_InvalidRequestException()
		{
			const string json = "{\"jsonrpc\": \"2.0\", \"params\": [\"2000-12-15T22:11:03\"], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Errors);
			Assert.Equal("1", result.Errors[0].Id);
			Assert.NotNull(result.Errors[0].Error);
			Assert.Equal((int)RpcErrorCode.InvalidRequest, result.Errors[0].Error.Code);
			Assert.False(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_MissingId_NoException()
		{
			const string json = "{\"method\": \"datetime\", \"jsonrpc\": \"2.0\", \"params\": [\"2000-12-15T22:11:03\"]}";
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Requests);
			Assert.Equal(default(RpcId), result.Requests[0].Id);
			Assert.Equal("datetime", result.Requests[0].Method);
			ParserTests.CompareParameters(new object[] { "2000-12-15T22:11:03" }, result.Requests[0].Parameters);
			Assert.False(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_MissingParams_NoException()
		{
			const string json = "{\"method\": \"datetime\",\"jsonrpc\": \"2.0\", \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);
			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Requests);
			Assert.Equal("1", result.Requests[0].Id);
			Assert.Equal("datetime", result.Requests[0].Method);
			Assert.Equal(default(RpcParameters), result.Requests[0].Parameters);
			Assert.False(result.IsBulkRequest);
		}

		[Fact]
		public void ParseRequests_ObjectParam_NoException()
		{
			const string json = "{\"method\": \"obj\", \"jsonrpc\": \"2.0\", \"params\": [{\"test\":1}], \"id\": \"1\"}";
			DefaultRpcParser parser = new DefaultRpcParser(new FakeLogger<DefaultRpcParser>(), Options.Create(new RpcServerConfiguration()));

			ParsingResult result = parser.ParseRequests(json);

			Assert.NotNull(result);
			Assert.Equal(1, result.RequestCount);
			Assert.Single(result.Requests);
			Assert.Equal("1", result.Requests[0].Id);
			Assert.Equal("obj", result.Requests[0].Method);
			ParserTests.CompareParameters(new object[] { new Obj { Test = 1 } }, result.Requests[0].Parameters);
			Assert.False(result.IsBulkRequest);
		}

		private class Obj
		{
			public int Test { get; set; }
		}
	}
}
