using JsonRpc.Router.Abstractions;
using Microsoft.Framework.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router.Defaults
{
	public class DefaultRpcParser : IRpcParser
	{
		public ILogger Logger { get; set; }
		public DefaultRpcParser(ILogger logger = null)
		{
			this.Logger = logger;
		}

		public bool MatchesRpcRoute(RpcRouteCollection routes, string requestUrl, out RpcRoute route)
		{
			if (routes == null)
			{
				throw new ArgumentNullException(nameof(routes));
			}
			if (requestUrl == null)
			{
				throw new ArgumentNullException(nameof(requestUrl));
			}
			this.Logger?.LogVerbose($"Attempting to match Rpc route for the request url '{requestUrl}'");
			RpcPath requestPath = RpcPath.Parse(requestUrl);
			RpcPath routePrefix = RpcPath.Parse(routes.RoutePrefix);
			
			foreach (RpcRoute rpcRoute in routes)
			{
				RpcPath routePath = RpcPath.Parse(rpcRoute.Name);
				routePath = routePrefix.Add(routePath);
				if (requestPath == routePath)
				{
					this.Logger?.LogVerbose($"Matched the request url '{requestUrl}' to the route '{rpcRoute.Name}'");
					route = rpcRoute;
					return true;
				}
			}
			this.Logger?.LogVerbose($"Failed to match the request url '{requestUrl}' to a route");
			route = null;
			return false;
		}

		public List<RpcRequest> ParseRequests(string jsonString)
		{
			this.Logger?.LogVerbose($"Attempting to parse Rpc request from the json string '{jsonString}'");
			List<RpcRequest> rpcRequests;
			if (string.IsNullOrWhiteSpace(jsonString))
			{
				throw new RpcInvalidRequestException("Json request was empty");
			}
			try
			{
				if (!DefaultRpcParser.IsSingleRequest(jsonString))
				{
					rpcRequests = JsonConvert.DeserializeObject<List<RpcRequest>>(jsonString);
				}
				else
				{
					rpcRequests = new List<RpcRequest>();
					RpcRequest rpcRequest = JsonConvert.DeserializeObject<RpcRequest>(jsonString);
					if (rpcRequest != null)
					{
						rpcRequests.Add(rpcRequest);
					}
				}
			}
			catch (Exception ex)
			{
				string errorMessage = "Unable to parse json request into an rpc format.";
#if DEBUG
				errorMessage += "\tException: " + ex.Message;
#endif
				throw new RpcInvalidRequestException(errorMessage);
			}

			if (rpcRequests == null || !rpcRequests.Any())
			{
				throw new RpcInvalidRequestException("No rpc json requests found");
			}
			this.Logger?.LogVerbose($"Successfully parsed {rpcRequests.Count} Rpc request(s)");
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

		private static bool IsSingleRequest(string jsonString)
		{
			if (string.IsNullOrEmpty(jsonString))
			{
				throw new RpcInvalidRequestException(nameof(jsonString));
			}
			for (int i = 0; i < jsonString.Length; i++)
			{
				char character = jsonString[i];
				switch (character)
				{
					case '{':
						return true;
					case '[':
						return false;
				}
			}
			return true;
		}
	}
}
