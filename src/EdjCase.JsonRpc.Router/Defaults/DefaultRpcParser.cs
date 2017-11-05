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
				//Only using JToken to get type, cant use it to deserialize because 
				//it ignores the settings https://github.com/JamesNK/Newtonsoft.Json/issues/862
				JToken token = JToken.Parse(jsonString);
				switch (token.Type)
				{
					case JTokenType.Array:
						isBulkRequest = true;
						rpcRequests = JsonConvert.DeserializeObject<List<RpcRequest>>(jsonString, jsonSerializerSettings);
						break;
					case JTokenType.Object:
						isBulkRequest = false;
						rpcRequests = new List<RpcRequest>();
						RpcRequest rpcRequest = JsonConvert.DeserializeObject<RpcRequest>(jsonString, jsonSerializerSettings);
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
			var uniqueIds = new HashSet<RpcId>();
			foreach (RpcRequest rpcRequest in rpcRequests.Where(r => r.Id.HasValue))
			{
				bool unique = uniqueIds.Add(rpcRequest.Id);
				if (!unique)
				{
					throw new RpcInvalidRequestException("Duplicate ids in batch requests are not allowed");
				}
			}
			return rpcRequests;
		}
	}
}
