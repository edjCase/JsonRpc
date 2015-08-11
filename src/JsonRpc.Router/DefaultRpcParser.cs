using JsonRpc.Router.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class DefaultRpcParser : IRpcParser
	{
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
			RpcPath requestPath = RpcPath.Parse(requestUrl);
			RpcPath routePrefix = RpcPath.Parse(routes.RoutePrefix);
			
			foreach (RpcRoute rpcRoute in routes)
			{
				RpcPath routePath = RpcPath.Parse(rpcRoute.Name);
				routePath = routePrefix.Add(routePath);
				if (requestPath == routePath)
				{
					route = rpcRoute;
					return true;
				}
			}
			route = null;
			return false;
		}

		public List<RpcRequest> ParseRequests(string jsonString)
		{
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
				string errorMessage = "Unable to parse json request into an rpc format";
#if DEBUG
				errorMessage += "\tException: " + ex.Message;
#endif
				throw new RpcInvalidRequestException(errorMessage);
			}

			if (rpcRequests == null || !rpcRequests.Any())
			{
				throw new RpcInvalidRequestException("No rpc json requests found");
			}
			HashSet<string> uniqueIds = new HashSet<string>();
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
