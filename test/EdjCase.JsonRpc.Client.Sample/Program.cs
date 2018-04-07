using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
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
			IRpcTransportClient transportClient = new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue));
			RpcClient client = new RpcClient(new Uri("http://localhost:62390/RpcApi/"), transportClient: transportClient);
			RpcRequest request = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
			RpcResponse response = await client.SendRequestAsync(request, "Strings");

			List<RpcRequest> requests = new List<RpcRequest>
			{
				request,
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test2" }, "Id2"),
				RpcRequest.WithParameterList("CharacterCount", new[] { "Test23" }, "Id3")
			};
			List<RpcResponse> bulkResponse = await client.SendBulkRequestAsync(requests, "Strings");

			IntegerFromSpace responseValue = response.GetResult<IntegerFromSpace>();
			if (responseValue == null)
			{
				Console.WriteLine("null");
			}
			else
			{
				Console.WriteLine(responseValue.Test);
			}

			var additionalHeaders = new List<(string, string)>
			{
				("Accept-Encoding", "gzip")
			};
			transportClient = new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue), headers: additionalHeaders);
			var compressedClient = new RpcClient(new Uri("http://localhost:62390/RpcApi/"), transportClient: transportClient);
			var compressedRequest = RpcRequest.WithParameterList("CharacterCount", new[] { "Test" }, "Id1");
			var compressedResponse = await compressedClient.SendRequestAsync(request, "Strings");
		}
	}


	public class IntegerFromSpace
	{
		public int Test { get; set; }
	}
}