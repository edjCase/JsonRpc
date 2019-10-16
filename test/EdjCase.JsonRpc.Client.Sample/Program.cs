using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Common;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace EdjCase.JsonRpc.Client.Sample
{
	public class Program
	{
		public static void Main(string[] args)
		{
			try
			{
				IntegrationTestRunner.RunAsync().Wait();
			}
			catch (AggregateException aEx)
			{
				foreach (Exception exception in aEx.InnerExceptions)
				{
					Console.WriteLine(exception.Message);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			Console.ReadLine();
		}

	}

	public static class IntegrationTestRunner
	{
		private const string url = "http://localhost:62390/RpcApi/";
		private static AuthenticationHeaderValue authHeaderValue { get; } = AuthenticationHeaderValue.Parse("Basic R2VrY3RlazpXZWxjMG1lIQ==");

		public static async Task RunAsync()
		{
			await IntegrationTestRunner.Test1();
			await IntegrationTestRunner.Test2();
			//await IntegrationTestRunner.Test3();
			await IntegrationTestRunner.Test4();
		}

		private static async Task Test1()
		{
			// var options = new WebSocketRpcTransportClientOptions();
			// string url = "ws://localhost:5000/WebSocket";
			//IRpcTransportClient transportClient = new WebSocketRpcTransportClient(Options.Create(options));


			IRpcTransportClient transportClient = new HttpRpcTransportClient(() => Task.FromResult(IntegrationTestRunner.authHeaderValue));
			RpcClient client = new RpcClient(new Uri(url), transportClient: transportClient);
			RpcRequest request = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
			RpcResponse<TestObject> response = await client.SendRequestAsync<TestObject>(request);

			if (response.Result.Test != 4)
			{
				throw new Exception("Test 1 failed.");
			}
		}

		private static async Task Test2()
		{
			IRpcTransportClient transportClient = new HttpRpcTransportClient(() => Task.FromResult(IntegrationTestRunner.authHeaderValue));
			RpcClient client = new RpcClient(new Uri(url), transportClient: transportClient);
			List<RpcRequest> requests = new List<RpcRequest>
			{
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1"),
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test2" }, "Id2"),
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test23" }, "Id3")
			};
			List<RpcResponse<TestObject>> bulkResponse = await client.SendBulkRequestAsync<TestObject>(requests);

			foreach (RpcResponse<TestObject> r in bulkResponse)
			{
				switch (r.Id.StringValue)
				{
					case "Id1":
						if (r.Result.Test != 4)
						{
							throw new Exception("Test 2.1 failed.");
						}
						break;
					case "Id2":
						if (r.Result.Test != 5)
						{
							throw new Exception("Test 2.2 failed.");
						}
						break;
					case "Id3":
						if (r.Result.Test != 6)
						{
							throw new Exception("Test 2.3 failed.");
						}
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(r.Id));
				}
			}
		}

		//private static async Task Test3()
		//{
		//TODO
		// var additionalHeaders = new List<(string, string)>
		// {
		// 	("Accept-Encoding", "gzip")
		// };
		// RpcRequest request = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
		// var transportClient = new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue), headers: additionalHeaders);
		// var compressedClient = new RpcClient(new Uri(TestRunner.url), transportClient: transportClient);
		// var compressedRequest = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
		// var compressedResponse = await compressedClient.SendRequestAsync(request, "Strings");
		//}


		private static async Task Test4()
		{

			IRpcTransportClient transportClient = new HttpRpcTransportClient(() => Task.FromResult(IntegrationTestRunner.authHeaderValue));
			RpcClient client = new RpcClient(new Uri(url), transportClient: transportClient);
			RpcRequest request = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
			RpcResponse<TestObject> response = await client.SendRequestAsync<TestObject>(request);

			if (response.Result.Test != 4)
			{
				throw new Exception("Test 1 failed.");
			}
		}
	}


	public class TestObject
	{
		public int Test { get; set; }
	}
}