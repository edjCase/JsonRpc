using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class SerializerTests
	{
		[Fact]
		public void ValidResponseSerialization()
		{
			var config = new RpcServerConfiguration();
			var serializer = new DefaultRpcResponseSerializer(Options.Create(config));

			const string expectedResponseString = "{\"id\":1,\"jsonrpc\":\"2.0\",\"result\":\"result\"}";
			var response = new RpcResponse(1, "result");
			string responseString = serializer.Serialize(response);

			Assert.Equal(expectedResponseString, responseString, ignoreCase: false, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
		}


		[Fact]
		public void ValidErrorResponseSerialization()
		{
			var config = new RpcServerConfiguration();
			var serializer = new DefaultRpcResponseSerializer(Options.Create(config));

			const string expectedResponseString = "{\"id\":2,\"jsonrpc\":\"2.0\",\"error\":{\"code\":2,\"message\":\"error\",\"data\":\"data\"}}";
			var response = new RpcResponse(2, new RpcError(2, "error", "data"));
			string responseString = serializer.Serialize(response);

			Assert.Equal(expectedResponseString, responseString, ignoreCase: false, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
		}


		[Fact]
		public void ValidBulkResponseSerialization()
		{
			var config = new RpcServerConfiguration();
			var serializer = new DefaultRpcResponseSerializer(Options.Create(config));

			const string expectedResponseString = "[{\"id\":1,\"jsonrpc\":\"2.0\",\"result\":\"result\"},{\"id\":2,\"jsonrpc\":\"2.0\",\"error\":{\"code\":2,\"message\":\"error\",\"data\":\"data\"}},{\"id\":3,\"jsonrpc\":\"2.0\",\"result\":\"result3\"}]";
			var response = new RpcResponse(1, "result");
			var errorResponse = new RpcResponse(2, new RpcError(2, "error", "data"));
			var response2 = new RpcResponse(3, "result3");
			string responseString = serializer.SerializeBulk(new[] { response, errorResponse, response2 });

			Assert.Equal(expectedResponseString, responseString, ignoreCase: false, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
		}
	}
}
