using System;
using System.Collections.Generic;
using System.Linq;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace EdjCase.JsonRpc.Router.Defaults
{
	/// <summary>
	/// Default Rpc parser that uses <see cref="Newtonsoft.Json"/>
	/// </summary>
	public class DefaultRpcParser : IRpcParser
	{
		/// <summary>
		/// Logger for logging Rpc parsing
		/// </summary>
		private ILogger<DefaultRpcParser> logger { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger">Optional logger for logging Rpc parsing</param>
		public DefaultRpcParser(ILogger<DefaultRpcParser> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// Indicates if the incoming request matches any predefined routes
		/// </summary>
		/// <param name="requestUrl">The current request url</param>
		/// <param name="route">The matching route corresponding to the request url if found, otherwise it is null</param>
		/// <param name="routeProvider">Provider that allows the retrieval of all configured routes</param>
		/// <returns>True if the request url matches any Rpc routes, otherwise False</returns>
		public bool MatchesRpcRoute(IRpcRouteProvider routeProvider, string requestUrl, out RpcRoute route)
		{
			if (requestUrl == null)
			{
				throw new ArgumentNullException(nameof(requestUrl));
			}
			this.logger?.LogDebug($"Attempting to match Rpc route for the request url '{requestUrl}'");
			RpcPath requestPath = RpcPath.Parse(requestUrl);
			this.logger?.LogTrace($"Request path: {requestPath}");
			
			foreach (RpcRoute rpcRoute in routeProvider.GetRoutes())
			{
				RpcPath routePath = RpcPath.Parse(rpcRoute.Name, routeProvider.BaseRequestPath);
				this.logger?.LogTrace($"Trying to match against route - Name: {rpcRoute.Name}, Path: {routePath}");
				if (requestPath == routePath)
				{
					this.logger?.LogDebug($"Matched the request url '{requestUrl}' to the route '{rpcRoute.Name}'");
					route = rpcRoute;
					return true;
				}

			}
			this.logger?.LogDebug($"Failed to match the request url '{requestUrl}' to a route");
			route = null;
			return false;
		}
		
		/// <summary>
		/// Parses all the requests from the json in the request
		/// </summary>
		/// <param name="jsonString">Json from the http request</param>
		/// <param name="jsonSerializerSettings">(Optional)Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <param name="isBulkRequest">If true, the request is a bulk request (even if there is only one)</param>
		/// <returns>List of Rpc requests that were parsed from the json</returns>
		public List<RpcRequest> ParseRequests(string jsonString, out bool isBulkRequest, JsonSerializerSettings jsonSerializerSettings = null)
		{
			this.logger?.LogDebug($"Attempting to parse Rpc request from the json string '{jsonString}'");
			List<RpcRequest> rpcRequests;
			if (string.IsNullOrWhiteSpace(jsonString))
			{
				throw new RpcInvalidRequestException("Json request was empty");
			}
			try
			{
				JToken token = JToken.Parse(jsonString);
				JsonSerializer serializer = JsonSerializer.Create(jsonSerializerSettings);
				switch (token.Type)
				{
					case JTokenType.Array:
						isBulkRequest = true;
						rpcRequests = token.ToObject<List<RpcRequest>>(serializer);
						break;
					case JTokenType.Object:
						isBulkRequest = false;
						rpcRequests = new List<RpcRequest>();
						RpcRequest rpcRequest = token.ToObject<RpcRequest>(serializer);
						if (rpcRequest != null)
						{
							rpcRequests.Add(rpcRequest);
						}
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(token.Type));
				}
			}
			catch (Exception ex) when (!(ex is RpcException))
			{
				string errorMessage = "Unable to parse json request into an rpc format.";
				this.logger?.LogException(ex, errorMessage);
				throw new RpcInvalidRequestException(errorMessage);
			}

			if (rpcRequests == null || !rpcRequests.Any())
			{
				throw new RpcInvalidRequestException("No rpc json requests found");
			}
			this.logger?.LogDebug($"Successfully parsed {rpcRequests.Count} Rpc request(s)");
			HashSet<object> uniqueIds = new HashSet<object>();
			foreach (RpcRequest rpcRequest in rpcRequests)
			{
				bool unique = uniqueIds.Add(rpcRequest.Id);
				if (!unique && rpcRequest.Id != null)
				{
					throw new RpcInvalidRequestException("Duplicate ids in batch requests are not allowed");
				}
			}
			return rpcRequests;
		}
	}
}
