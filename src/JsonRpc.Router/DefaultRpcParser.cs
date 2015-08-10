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
		private string RoutePrefix { get; }
		private List<RpcSection> Sections { get; }
		public DefaultRpcParser(string routePrefix, List<RpcSection> sections)
		{
			this.RoutePrefix = routePrefix;
			this.Sections = sections;
		}

		public bool MatchesRpcRoute(string requestUrl, out string section)
		{
			PathString pathString = new PathString(requestUrl);
			PathString remainingPath;
			bool isRpcRoute = pathString.StartsWithSegments(this.RoutePrefix, out remainingPath);
			if (!isRpcRoute)
			{
				section = null;
				return false;
			}
			string[] pathComponents = remainingPath.Value?.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if(pathComponents == null || !pathComponents.Any())
			{
				section = null;
				return false;
			}

			section = pathComponents.First();
			if (string.IsNullOrWhiteSpace(section))
			{
				section = null;
				return false;
			}
			return true;
		}

		public List<RpcRequest> ParseRequests(string jsonString)
		{
			List<RpcRequest> rpcRequests;
			if (string.IsNullOrWhiteSpace(jsonString))
			{
				throw new InvalidRpcRequestException("Json request was empty");
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
				errorMessage += "\n\tException:" + ex.Message;
#endif
				throw new InvalidRpcRequestException(errorMessage);
			}

			if (!rpcRequests.Any())
			{
				throw new InvalidRpcRequestException("No rpc json requests found");
			}
			return rpcRequests;
		}

		private bool IsSingleRequest(string jsonString)
		{
			if (jsonString == null || jsonString.Length < 1)
			{
				throw new ArgumentNullException(nameof(jsonString));
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
