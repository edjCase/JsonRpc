using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
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
				Program.Run().Wait();
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

		public static async Task Run()
		{
			AuthenticationHeaderValue authHeaderValue = AuthenticationHeaderValue.Parse("Basic R2VrY3RlazpXZWxjMG1lIQ==");
			//IRpcTransportClient transportClient = new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue));
			//string url = "http://localhost:62390/RpcApi/"
			var options = new WebSocketRpcTransportClientOptions();
			string url = "ws://localhost:5000/WebSocket";
			IRpcTransportClient transportClient = new WebSocketRpcTransportClient(Options.Create(options));
			RpcClient client = new RpcClient(new Uri(url), transportClient: transportClient);
			RpcRequest request = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
			RpcResponse<IntegerFromSpace> response = await client.SendRequestAsync<IntegerFromSpace>(request);

			if (response.Result.Test != 4)
			{
				throw new Exception("Test 1 failed.");
			}
			List<RpcRequest> requests = new List<RpcRequest>
			{
				request,
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test2" }, "Id2"),
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test23" }, "Id3")
			};
			List<RpcResponse<IntegerFromSpace>> bulkResponse = await client.SendBulkRequestAsync<IntegerFromSpace>(requests);

			foreach (RpcResponse<IntegerFromSpace> r in bulkResponse)
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
			// var additionalHeaders = new List<(string, string)>
			// {
			// 	("Accept-Encoding", "gzip")
			// };
			// transportClient = new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue), headers: additionalHeaders);
			// var compressedClient = new RpcClient(new Uri("http://localhost:62390/RpcApi/"), transportClient: transportClient);
			// var compressedRequest = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
			// var compressedResponse = await compressedClient.SendRequestAsync(request, "Strings");
		}
	}


	public class IntegerFromSpace
	{
		public int Test { get; set; }
	}
}