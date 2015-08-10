using JsonRpc.Router.Abstractions;
using Microsoft.AspNet.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class DefaultRpcParser : IRpcParser
	{
		private PathString RoutePrefix { get; }
		private RpcRouteCollection Routes { get; }
		public DefaultRpcParser(string routePrefix, RpcRouteCollection routes)
		{
			this.RoutePrefix = routePrefix;
			this.Routes = routes;
		}

		public bool MatchesRpcRoute(string requestUrl, out RpcRoute route)
		{
			if(requestUrl == null)
			{
				throw new ArgumentNullException(nameof(requestUrl));
			}
			if (!requestUrl.StartsWith("/"))
			{
				requestUrl = "/" + requestUrl;
			}
			PathString requestPath = new PathString(requestUrl);

			foreach (RpcRoute rpcRoute in this.Routes)
			{
				PathString routePath = this.RoutePrefix.Add(new PathString("/" + rpcRoute.Name));
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
				if (!this.IsSingleRequest(jsonString))
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
				if (!unique)
				{
					throw new RpcInvalidRequestException("Duplicate ids in batch requests are not allowed");
				}
			}
			return rpcRequests;
		}

		private bool IsSingleRequest(string jsonString)
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
