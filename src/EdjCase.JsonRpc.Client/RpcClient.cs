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
using EdjCase.JsonRpc.Core.Tools;
using System.Diagnostics;

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
		/// Client to send and receive the serialized rpc requests from the server
		/// </summary>
		public IRpcTransportClient TransportClient { get; }
		/// <summary>
		/// Json serializer for serializing requests and deserializing responses
		/// </summary>
		public IRequestSerializer JsonSerializer { get; }

		public RpcEvents Events { get; }

		/// <param name="baseUrl">Base url for the rpc server</param>
		/// <param name="authHeaderValue">Http authentication header for rpc request</param>
		/// <param name="jsonSerializer">(Optional) Json serializer for serializing requests and deserializing responses. Defaults to built in serializer</param>
		/// <param name="encoding">(Optional)Encoding type for request. Defaults to UTF-8</param>
		/// <param name="contentType">(Optional)Content type header for the request. Defaults to application/json</param>
		/// <param name="headers">(Optional)Extra headers</param>
		public RpcClient(Uri baseUrl,
			IRequestSerializer jsonSerializer = null,
			IRpcTransportClient transportClient = null,
			RpcEvents events = null)
		{
			this.BaseUrl = baseUrl;
			this.JsonSerializer = jsonSerializer ?? new DefaultRequestJsonSerializer();
			this.TransportClient = transportClient ?? new HttpRpcTransportClient();
			this.Events = events ?? new RpcEvents();
		}


		/// <summary>
		/// Sends the specified rpc request to the server
		/// </summary>
		/// <param name="request">Single rpc request that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="resultType">(Optional) If specified will be the deserialization type for the response result. Otherwise the result will be a json string.</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestAsync(RpcRequest request, string route = null, Type resultType = null)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			if (!request.Id.HasValue)
			{
				throw new InvalidOperationException("Cannot call a method expecting a response and not rpc id specified in the request.");
			}
			return (await this.SendAsync(new[] { request }, route, ResolveType).ConfigureAwait(false)).SingleOrDefault();
			//functions
			Type ResolveType(RpcId id)
			{
				return resultType;
			}
		}

		/// <summary>
		/// Sends the specified rpc request to the server
		/// </summary>
		/// <param name="request">Single rpc request that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <typeparam name="T">The deserialization type for the response result.</typeparam>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse<T>> SendRequestAsync<T>(RpcRequest request, string route = null)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			return RpcResponse<T>.FromResponse(await this.SendRequestAsync(request, route, typeof(T)).ConfigureAwait(false));
		}



		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramList">List of parameters (in order) for the rpc method</param>
		/// <returns>The rpc response for the sent request</returns>
		public Task<RpcResponse<T>> SendRequestAsync<T>(string method, string route, params object[] paramList)
		{
			return this.SendRequestWithListAsync<T>(method, route, paramList);
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramList">List of parameters (in order) for the rpc method</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestAsync(string method, string route, params object[] paramList)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new ArgumentNullException(nameof(method));
			}
			RpcRequest request = RpcRequest.WithParameterList(method, paramList, Guid.NewGuid().ToString());
			return await this.SendRequestAsync(request, route).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramList">List of parameters (in order) for the rpc method</param>
		/// <param name="resultType">(Optional) If specified will be the deserialization type for the response result. Otherwise the result will be a json string.</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestWithListAsync(string method, string route, IList<object> paramList, Type resultType = null)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new ArgumentNullException(nameof(method));
			}
			RpcRequest request = RpcRequest.WithParameterList(method, paramList, Guid.NewGuid().ToString());
			return await this.SendRequestAsync(request, route, resultType).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramList">List of parameters (in order) for the rpc method</param>
		/// <typeparam name="T">The deserialization type for the response result.</typeparam>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse<T>> SendRequestWithListAsync<T>(string method, string route, IList<object> paramList)
		{
			return RpcResponse<T>.FromResponse(await this.SendRequestWithListAsync(method, route, paramList, typeof(T)));
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramMap">Map of parameters for the rpc method</param>
		/// <param name="resultType">(Optional) If specified will be the deserialization type for the response result. Otherwise the result will be a json string.</param>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse> SendRequestWithMapAsync(string method, string route, IDictionary<string, object> paramMap, Type resultType = null)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new ArgumentNullException(nameof(method));
			}
			RpcRequest request = RpcRequest.WithParameterMap(method, paramMap, Guid.NewGuid().ToString());
			return await this.SendRequestAsync(request, route, resultType).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramMap">Map of parameters for the rpc method</param>
		/// <typeparam name="T">The deserialization type for the response result.</typeparam>
		/// <returns>The rpc response for the sent request</returns>
		public async Task<RpcResponse<T>> SendRequestWithMapAsync<T>(string method, string route, IDictionary<string, object> paramMap)
		{
			return RpcResponse<T>.FromResponse(await this.SendRequestWithMapAsync(method, route, paramMap, typeof(T)));
		}

		/// <summary>
		/// Sends the specified rpc requests to the server
		/// </summary>
		/// <param name="requests">Multiple rpc requests that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the requests method call is not located at the base route</param>
		/// <param name="resultTypeResolver">(Optional) Function that will determine the deserialization type for the result based on request id. Otherwise the result will be a json string.</param>
		/// <returns>The rpc responses for the sent requests</returns>
		public async Task<List<RpcResponse>> SendBulkRequestAsync(IEnumerable<RpcRequest> requests, string route = null, Func<RpcId, Type> resultTypeResolver = null)
		{
			if (requests == null)
			{
				throw new ArgumentNullException(nameof(requests));
			}
			List<RpcRequest> requestList = requests.ToList();
			return await this.SendAsync(requestList, route, resultTypeResolver).ConfigureAwait(false);
		}

		/// <summary>
		/// Sends the specified rpc requests to the server
		/// </summary>
		/// <param name="requests">Multiple rpc requests that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the requests method call is not located at the base route</param>
		/// <param name="resultTypeResolver">(Optional) Function that will determine the deserialization type for the result based on request id. Otherwise the result will be a json string.</param>
		/// <returns>The rpc responses for the sent requests</returns>
		public async Task<List<RpcResponse<T>>> SendBulkRequestAsync<T>(IEnumerable<RpcRequest> requests, string route = null)
		{
			if (requests == null)
			{
				throw new ArgumentNullException(nameof(requests));
			}
			List<RpcRequest> requestList = requests.ToList();
			return (await this.SendAsync(requestList, route, (id) => typeof(T)).ConfigureAwait(false))
				.Select(RpcResponse<T>.FromResponse)
				.ToList();
		}

		/// <summary>
		/// Sends the a http request to the server, posting the request in json format
		/// </summary>
		/// <param name="request">Request object that will goto the rpc server</param>
		/// <param name="deserializer">Function to deserialize the json response</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="resultTypeResolver">(Optional) Function that will determine the deserialization type for the result based on request id. Otherwise the result will be a json string.</param>
		/// <returns>The response for the sent request</returns>
		private async Task<List<RpcResponse>> SendAsync(IList<RpcRequest> requests, string route = null, Func<RpcId, Type> resultTypeResolver = null)
		{
			if (!requests.Any())
			{
				throw new ArgumentException("There must be at least one rpc request when sending.");
			}
			try
			{
				string requestJson;
				if (requests.Count == 1)
				{
					requestJson = this.JsonSerializer.Serialize(requests.Single());
				}
				else
				{
					requestJson = this.JsonSerializer.SerializeBulk(requests);
				}
				Uri uri = new Uri(this.BaseUrl, route);
				var requestContext = new RequestEventContext();
				ResponseEventContext responseContext = null;
				await this.Events.OnRequestStartAsync?.Invoke(requestContext);

				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					string responseJson = await this.TransportClient.SendRequestAsync(uri, requestJson);
					stopwatch.Stop();

					if (string.IsNullOrWhiteSpace(responseJson))
					{
						throw new RpcClientParseException("Server did not return a rpc response, just an empty body.");
					}
					List<RpcResponse> responses = null;
					try
					{
						if (requests.Count == 1)
						{
							Type resultType = resultTypeResolver?.Invoke(requests.First().Id);
							RpcResponse response = this.JsonSerializer.Deserialize(responseJson, resultType);
							responses = new List<RpcResponse> { response };
						}
						else
						{
							responses = this.JsonSerializer.DeserializeBulk(responseJson, resultTypeResolver);
						}
						responseContext = new ResponseEventContext(stopwatch.Elapsed, responses);
						return responses;
					}
					catch (Exception ex)
					{
						responseContext = new ResponseEventContext(stopwatch.Elapsed, responses, ex);
						throw new RpcClientParseException($"Unable to parse response from server: '{responseJson}'", ex);
					}
				}
				finally
				{
					await this.Events.OnRequestCompleteAsync?.Invoke(responseContext, requestContext);
				}

			}
			catch (Exception ex) when (!(ex is RpcClientException) && !(ex is RpcException))
			{
				throw new RpcClientUnknownException("Error occurred when trying to send rpc requests(s)", ex);
			}
		}

		public static RpcClient CreateWithHttpClient(Uri baseUrl,
			Func<Task<AuthenticationHeaderValue>> authHeaderValueFactory = null,
			Encoding encoding = null,
			string contentType = null,
			IEnumerable<(string, string)> headers = null,
			IStreamCompressor streamCompressor = null,
			IRequestSerializer jsonSerializer = null,
			RpcEvents events = null)
		{
			var transportClient = new HttpRpcTransportClient(authHeaderValueFactory, encoding, contentType, headers, streamCompressor);
			return new RpcClient(baseUrl, jsonSerializer, transportClient, events);
		}

		public static RpcClient CreateWithHttpClientAndJsonSerializationSettings(Uri baseUrl,
			JsonSerializerSettings jsonSerializerSettings,
			Func<Task<AuthenticationHeaderValue>> authHeaderValueFactory = null,
			Encoding encoding = null,
			string contentType = null,
			IEnumerable<(string, string)> headers = null,
			IStreamCompressor streamCompressor = null,
			IErrorDataSerializer errorDataSerializer = null,
			RpcEvents events = null)
		{
			var transportClient = new HttpRpcTransportClient(authHeaderValueFactory, encoding, contentType, headers, streamCompressor);
			var jsonSerializer = new DefaultRequestJsonSerializer(errorDataSerializer, jsonSerializerSettings);
			return new RpcClient(baseUrl, jsonSerializer, transportClient, events);
		}

		public static HttpRpcClientBuilder CreateHttpTransportClientBuilder(Uri baseUrl)
		{
			return new HttpRpcClientBuilder(baseUrl);
		}



	}

}
