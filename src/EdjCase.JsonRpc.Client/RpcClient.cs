using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using EdjCase.JsonRpc.Common.Tools;
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
		private IRpcTransportClient transportClient { get; }
		/// <summary>
		/// Json serializer for serializing requests and deserializing responses
		/// </summary>
		private IRequestSerializer requestSerializer { get; }

		public RpcEvents Events { get; }

		internal RpcClient(Uri baseUrl,
			IRpcTransportClient transportClient,
			IRequestSerializer requestSerializer,
			RpcEvents events)
		{
			this.BaseUrl = baseUrl;
			this.transportClient = transportClient ?? throw new ArgumentNullException(nameof(transportClient));
			this.requestSerializer = requestSerializer ?? throw new ArgumentNullException(nameof(requestSerializer));
			this.Events = events ?? throw new ArgumentNullException(nameof(events));
		}

		/// <summary>
		/// Sends the specified rpc request to the server (Wrapper for other SendRequestAsync)
		/// </summary>
		/// <param name="method">Rpc method that is to be called</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <param name="paramList">List of parameters (in order) for the rpc method</param>
		/// <returns>The rpc response for the sent request</returns>
		public Task<RpcResponse<T>> SendAsync<T>(string method, string? route, params object[] paramList)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new ArgumentNullException(nameof(method));
			}
			RpcRequest request = RpcRequest.WithParameterList(method, parameterList: paramList, id: new RpcId(1));
			return this.SendAsync<T>(request, route);
		}

		/// <summary>
		/// Sends the specified rpc request to the server
		/// </summary>
		/// <param name="request">Single rpc request that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// <typeparam name="TResponse">The deserialization type for the response result.</typeparam>
		/// <returns>The rpc response for the sent request</returns>
		public Task<RpcResponse<TResponse>> SendAsync<TResponse>(RpcRequest request, string? route = null)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			var wrapper = new SingleRequestWrapper<TResponse>(request);
			return this.SendAsync(wrapper, route);
		}

		/// <summary>
		/// Sends the a http request to the server, posting the request in json format
		/// </summary>
		/// <param name="bulkRequest">Bulk request object that will goto the rpc server</param>
		/// <param name="route">(Optional) Route that will append to the base url if the request method call is not located at the base route</param>
		/// 
		public Task<RpcBulkResponse> SendAsync(RpcBulkRequest bulkRequest, string? route = null)
		{
			if (bulkRequest == null)
			{
				throw new ArgumentNullException(nameof(bulkRequest));
			}
			var wrapper = new BulkRequestWrapper(bulkRequest);
			return this.SendAsync(wrapper, route);
		}

		private async Task<TResponse> SendAsync<TResponse>(RequestWrapper<TResponse> request, string? route = null)
		{
			try
			{
				string requestJson = request.Serialize(this.requestSerializer);
				Uri uri = new Uri(this.BaseUrl, route);
				List<RpcRequest> requests = request.GetRequests();
				var requestContext = new RequestEventContext(route, requests, requestJson);
				ResponseEventContext? responseContext = null;
				if (this.Events.OnRequestStartAsync != null)
				{
					await this.Events.OnRequestStartAsync(requestContext);
				}

				List<RpcResponse>? responses = null;
				string responseJson;
				Exception? error = null;
				responseJson = await this.transportClient.SendRequestAsync(uri, requestJson);
				try
				{
					if (string.IsNullOrWhiteSpace(responseJson))
					{
						throw new RpcClientParseException("Server did not return a rpc response, just an empty body.");
					}
					try
					{
						TResponse returnValue;
						(responses, returnValue) = request.Deserialize(responseJson, this.requestSerializer);
						responseContext = new ResponseEventContext(responseJson, responses);
						return returnValue;
					}
					catch (Exception ex)
					{
						error = ex;
						throw new RpcClientParseException($"Unable to parse response from server: '{responseJson}'", ex);
					}
				}
				finally
				{
					if (this.Events.OnRequestCompleteAsync != null)
					{
						if(responseContext == null)
						{
							responseContext = new ResponseEventContext(responseJson, responses, error);
						}
						await this.Events.OnRequestCompleteAsync(responseContext, requestContext);
					}
				}

			}
			catch (Exception ex) when (!(ex is RpcClientException) && !(ex is RpcException))
			{
				throw new RpcClientUnknownException("Error occurred when trying to send rpc request(s)", ex);
			}
		}

		public static HttpRpcClientBuilder Builder(Uri baseUrl)
		{
			return new HttpRpcClientBuilder(baseUrl);
		}


		private abstract class RequestWrapper<TResponse>
		{
			internal abstract string Serialize(IRequestSerializer requestSerializer);
			internal abstract List<RpcRequest> GetRequests();
			internal abstract (List<RpcResponse>, TResponse) Deserialize(string responseJson, IRequestSerializer requestSerializer);
		}
		private class SingleRequestWrapper<TResponse> : RequestWrapper<RpcResponse<TResponse>>
		{
			private RpcRequest request { get; }
			public SingleRequestWrapper(RpcRequest request)
			{
				this.request = request;
			}

			internal override string Serialize(IRequestSerializer requestSerializer)
			{
				return requestSerializer.Serialize(this.request);
			}

			internal override List<RpcRequest> GetRequests()
			{
				return new List<RpcRequest> { this.request };
			}

			internal override (List<RpcResponse>, RpcResponse<TResponse>) Deserialize(string responseJson, IRequestSerializer requestSerializer)
			{
				var typeMap = new Dictionary<RpcId, Type> { [this.request.Id] = typeof(TResponse) };
				var responses = requestSerializer.Deserialize(responseJson, typeMap);
				return (responses, RpcResponse<TResponse>.FromResponse(responses.Single()));
			}
		}
		private class BulkRequestWrapper : RequestWrapper<RpcBulkResponse>
		{
			private RpcBulkRequest request { get; }
			public BulkRequestWrapper(RpcBulkRequest request)
			{
				this.request = request;
			}

			internal override (List<RpcResponse>, RpcBulkResponse) Deserialize(string responseJson, IRequestSerializer requestSerializer)
			{
				IDictionary<RpcId, Type> typeMap = this.request.GetTypeMap();
				var responses =  requestSerializer.Deserialize(responseJson, typeMap);
				return (responses, RpcBulkResponse.FromResponses(responses));
			}

			internal override List<RpcRequest> GetRequests()
			{
				return this.request.GetRequests();
			}

			internal override string Serialize(IRequestSerializer requestSerializer)
			{
				return requestSerializer.SerializeBulk(this.request.GetRequests());
			}
		}
	}

}
