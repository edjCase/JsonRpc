using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace EdjCase.JsonRpc.Client
{

	/// <summary>
	/// A client for making rpc requests and receiving rpc responses
	/// </summary>
	public class RpcClient
	{
		/// <summary>
		/// Base url for the rpc server
		/// </summary>
		public Uri BaseUrl { get; }
		/// <summary>
		/// Authentication header value for the rpc request being sent. If the server requires
		/// authentication this requires a value. Otherwise it can be null
		/// </summary>
		public AuthenticationHeaderValue AuthHeaderValue { get; set; }
		/// <summary>
		/// Json serialization settings that will be used in serialization and deserialization
		/// for rpc requests
		/// </summary>
		public JsonSerializerSettings JsonSerializerSettings { get; set; }
		/// <summary>
		/// Request encoding type for json content. If null, will default to UTF-8 
		/// </summary>
		public Encoding Encoding { get; set; }
		/// <summary>
		/// Request content type for json content. If null, will default to application/json
		/// </summary>
		public string ContentType { get; set; }

		/// <param name="baseUrl">Base url for the rpc server</param>
		/// <param name="authHeaderValue">Http authentication header for rpc request</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		public RpcClient(Uri baseUrl, AuthenticationHeaderValue authHeaderValue = null, JsonSerializerSettings jsonSerializerSettings = null, 
			Encoding encoding = null, string contentType = null)
		{
			this.BaseUrl = baseUrl;
			this.AuthHeaderValue = authHeaderValue;
			this.JsonSerializerSettings = jsonSerializerSettings;
			this.Encoding = encoding;
			this.ContentType = contentType;
		}

		/// <summary>
		/// Sends the specified rpc request to the server
		/// </summary>
		/// <param name="request">Single rpc request that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestAsync(RpcRequest request, string route = null)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			return await this.SendAsync<RpcRequest, RpcResponse>(request, route).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramList">List of parameters (in order) for the rpc method</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestAsync(string method, string route = null, params object[] paramList)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new ArgumentNullException(nameof(method));
			}
			RpcRequest request= new RpcRequest(Guid.NewGuid().ToString(), method, paramList);
			return await this.SendRequestAsync(request, route).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc requests to the server
		/// </summary>
		/// <param name="requests">Multiple rpc requests that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the requests method call is not located at the base route</param>
		/// <returns>The rpc responses for the sent requests</returns>
		public async Task<List<RpcResponse>> SendBulkRequestAsync(IEnumerable<RpcRequest> requests, string route = null)
		{
			if (requests == null)
			{
				throw new ArgumentNullException(nameof(requests));
			}
			List<RpcRequest> requestList = requests.ToList();
			return await this.SendAsync<List<RpcRequest>, List<RpcResponse>>(requestList, route).ConfigureAwait(false); ;
		}

		/// <summary>
		/// Sends the a http request to the server, posting the request in json format
		/// </summary>
		/// <param name="request">Request object that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <returns>The response for the sent request</returns>
		private async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, string route = null)
		{
			try
			{
				using (HttpClient httpClient = this.GetHttpClient())
				{
					httpClient.BaseAddress = this.BaseUrl;

					string rpcRequestJson = JsonConvert.SerializeObject(request, this.JsonSerializerSettings);
					HttpContent httpContent = new StringContent(rpcRequestJson, this.Encoding ?? Encoding.UTF8, this.ContentType ?? "application/json");
					HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(route, httpContent).ConfigureAwait(false);
					httpResponseMessage.EnsureSuccessStatusCode();

					string responseJson = await httpResponseMessage.Content.ReadAsStringAsync();

					try
					{
						return JsonConvert.DeserializeObject<TResponse>(responseJson, this.JsonSerializerSettings);
					}
					catch (JsonSerializationException)
					{
						RpcResponse rpcResponse = JsonConvert.DeserializeObject<RpcResponse>(responseJson, this.JsonSerializerSettings);
						if (rpcResponse == null)
						{
							throw new RpcClientUnknownException($"Unable to parse response from the rpc server. Response Json: {responseJson}");
						}
						throw rpcResponse.Error.CreateException();
					}
				}
			}
			catch (Exception ex) when(!(ex is RpcClientException) && !(ex is RpcException))
			{
				throw new RpcClientUnknownException("Error occurred when trying to send rpc requests(s)", ex);
			}
		}

		private HttpClient GetHttpClient()
		{
			HttpClient httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Authorization = this.AuthHeaderValue;
			return httpClient;
		}
	}
}
