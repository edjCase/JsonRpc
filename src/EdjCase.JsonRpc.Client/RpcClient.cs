using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
		/// Authentication header value factory for the rpc request being sent. If the server requires
		/// authentication this requires a value. Otherwise it can be null
		/// </summary>
		public Func<Task<AuthenticationHeaderValue>> AuthHeaderValueFactory { get; set; }
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
		/// <summary>
		/// Add headers to the underlying http client
		/// </summary>
		public List<KeyValuePair<string, string>> Headers { get; set; }

		private JsonSerializer jsonSerializerCache { get; set; }

		/// <param name="baseUrl">Base url for the rpc server</param>
		/// <param name="authHeaderValue">Http authentication header for rpc request</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <param name="encoding">(Optional)Encoding type for request. Defaults to UTF-8</param>
		/// <param name="contentType">(Optional)Content type header for the request. Defaults to application/json</param>
		/// <param name="headers">(Optional)Extra headers</param>
		public RpcClient(Uri baseUrl, AuthenticationHeaderValue authHeaderValue = null, JsonSerializerSettings jsonSerializerSettings = null,
			Encoding encoding = null, string contentType = null, List<KeyValuePair<string, string>> headers = null)
		{
			this.BaseUrl = baseUrl;
			this.AuthHeaderValueFactory = () => Task.FromResult(authHeaderValue);
			this.JsonSerializerSettings = jsonSerializerSettings;
			this.Encoding = encoding;
			this.ContentType = contentType;
			this.Headers = headers;
		}

		/// <param name="baseUrl">Base url for the rpc server</param>
		/// <param name="authHeaderValueFactory">Http authentication header factory for rpc request</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <param name="encoding">(Optional)Encoding type for request. Defaults to UTF-8</param>
		/// <param name="contentType">(Optional)Content type header for the request. Defaults to application/json</param>
		/// <param name="headers">(Optional)Extra headers</param>
		public RpcClient(Uri baseUrl, Func<Task<AuthenticationHeaderValue>> authHeaderValueFactory = null, JsonSerializerSettings jsonSerializerSettings = null,
			Encoding encoding = null, string contentType = null, List<KeyValuePair<string, string>> headers = null)
		{
			this.BaseUrl = baseUrl;
			this.AuthHeaderValueFactory = authHeaderValueFactory;
			this.JsonSerializerSettings = jsonSerializerSettings;
			this.Encoding = encoding;
			this.ContentType = contentType;
			this.Headers = headers;
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
			return await this.SendAsync(request, this.DeserilizeSingleRequest, route).ConfigureAwait(false);
		}

		private RpcResponse DeserilizeSingleRequest(string json)
		{
			JToken token = JToken.Parse(json);
			JsonSerializer serializer = JsonSerializer.Create(this.JsonSerializerSettings);
			switch (token.Type)
			{
				case JTokenType.Object:
					RpcResponse response = token.ToObject<RpcResponse>(serializer);
					return response;
				case JTokenType.Array:
					return token.ToObject<List<RpcResponse>>(serializer).SingleOrDefault();
				default:
					throw new Exception("Cannot parse rpc response from server.");
			}
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
			JsonSerializer jsonSerializer = this.GetJsonSerializer();
			RpcRequest request = RpcRequest.WithParameterList(Guid.NewGuid().ToString(), method, paramList, jsonSerializer);
			return await this.SendRequestAsync(request, route).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramMap">Map of parameters for the rpc method</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestAsync(string method, string route, Dictionary<string, object> paramMap)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new ArgumentNullException(nameof(method));
			}
			JsonSerializer jsonSerializer = this.GetJsonSerializer();
			RpcRequest request = RpcRequest.WithParameterMap(Guid.NewGuid().ToString(), method, paramMap, jsonSerializer);
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
			return await this.SendAsync(requestList, this.DeserilizeBulkRequests, route).ConfigureAwait(false);
		}

		private List<RpcResponse> DeserilizeBulkRequests(string json)
		{
			JToken token = JToken.Parse(json);
			JsonSerializer serializer = JsonSerializer.Create(this.JsonSerializerSettings);
			switch (token.Type)
			{
				case JTokenType.Object:
					RpcResponse response = token.ToObject<RpcResponse>(serializer);
					return new List<RpcResponse> { response };
				case JTokenType.Array:
					return token.ToObject<List<RpcResponse>>(serializer);
				default:
					throw new Exception("Cannout deserilize rpc response from server.");
			}
		}

		/// <summary>
		/// Sends the a http request to the server, posting the request in json format
		/// </summary>
		/// <param name="request">Request object that will goto the rpc server</param>
		/// <param name="deserializer">Function to deserialize the json response</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <returns>The response for the sent request</returns>
		private async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, Func<string, TResponse> deserializer, string route = null)
		{
			try
			{
				using (HttpClient httpClient = await this.GetHttpClientAsync())
				{
					httpClient.BaseAddress = this.BaseUrl;

					string rpcRequestJson = JsonConvert.SerializeObject(request, this.JsonSerializerSettings);
					HttpContent httpContent = new StringContent(rpcRequestJson, this.Encoding ?? Encoding.UTF8, this.ContentType ?? "application/json");
					HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(route, httpContent).ConfigureAwait(false);
					httpResponseMessage.EnsureSuccessStatusCode();

					string responseJson = null;
					try
					{
						responseJson = await this.HandleResponse(httpResponseMessage);
					}
					catch (JsonSerializationException)
					{
						throw new RpcClientUnknownException($"Unable to decode response from the rpc server. Response Json: {responseJson}");
					}

					try
					{
						return deserializer(responseJson);
					}
					catch (JsonSerializationException)
					{
						throw new RpcClientUnknownException($"Unable to parse response from the rpc server. Response Json: {responseJson}");
					}
				}
			}
			catch (Exception ex) when (!(ex is RpcClientException) && !(ex is RpcException))
			{
				throw new RpcClientUnknownException("Error occurred when trying to send rpc requests(s)", ex);
			}
		}

		private async Task<HttpClient> GetHttpClientAsync()
		{
			HttpClient httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Authorization = await this.AuthHeaderValueFactory();

			//attach any extra headers that have been passed
			if (this.Headers != null && this.Headers.Any())
			{
				foreach (var header in this.Headers)
				{
					httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
				}
			}

			return httpClient;
		}

		/// <summary>
		/// Handles the response.
		/// </summary>
		/// <param name="httpResponseMessage">The HTTP response message.</param>
		/// <returns></returns>
		private async Task<string> HandleResponse(HttpResponseMessage httpResponseMessage)
		{
			string responseJson = null;
			httpResponseMessage.Content.Headers.TryGetValues("Content-Encoding", out var encodings);

			//handle compressions
			if (encodings != null && encodings.Any())
			{
				foreach (string encoding in encodings)
				{
					bool haveType = Enum.TryParse(encoding, true, out CompressionType compressionType);
					if (!haveType)
					{
						continue;
					}

					var data = this.DecompressText(await httpResponseMessage.Content.ReadAsStreamAsync(), compressionType);
					responseJson = Encoding.UTF8.GetString(data, 0, data.Length);
					break;
				}
			}
			// uncompressed, standard response
			else
			{
				responseJson = await httpResponseMessage.Content.ReadAsStringAsync();
			}

			return responseJson;
		}

		/// <summary>
		/// Decompresses the input stream to byte[].
		/// </summary>
		/// <param name="inputStream">The input stream.</param>
		/// <param name="compressionType">Type of the compression.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">compressionType - null</exception>
		private byte[] DecompressText(Stream inputStream, CompressionType compressionType)
		{
			switch (compressionType)
			{
				case CompressionType.Gzip:
					using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
					using (var resultStream = new MemoryStream())
					{
						zipStream.CopyTo(resultStream);
						return resultStream.ToArray();
					}
				case CompressionType.Deflate:
					using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
					using (var resultStream = new MemoryStream())
					{
						deflateStream.CopyTo(resultStream);
						return resultStream.ToArray();
					}
				default:
					throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null);
			}
		}

		private JsonSerializer GetJsonSerializer()
		{
			if(this.jsonSerializerCache == null)
			{
				if(this.JsonSerializerSettings != null)
				{
					this.jsonSerializerCache = JsonSerializer.Create(this.JsonSerializerSettings);
				}
				else
				{
					this.jsonSerializerCache = JsonSerializer.CreateDefault();
				}
			}
			return this.jsonSerializerCache;
		}

		public enum CompressionType
		{
			Gzip,
			Deflate
		}
	}
}
