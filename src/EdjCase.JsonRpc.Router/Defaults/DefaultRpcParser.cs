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
using System.IO;
using Edjcase.JsonRpc.Router;

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
		/// <param name="isBulkRequest">If true, the request is a bulk request (even if there is only one)</param>
		/// <returns>List of Rpc requests that were parsed from the json</returns>
		public ParsingResult ParseRequests(string jsonString)
		{
			this.logger?.LogDebug($"Attempting to parse Rpc request from the json string '{jsonString}'");
			List<RpcRequestParseResult> rpcRequests;
			if (string.IsNullOrWhiteSpace(jsonString))
			{
				throw new RpcException(RpcErrorCode.InvalidRequest, "Json request was empty");
			}
			bool isBulkRequest;
			try
			{
				using (JsonReader jsonReader = new JsonTextReader(new StringReader(jsonString)))
				{
					//Fixes the date parsing issue https://github.com/JamesNK/Newtonsoft.Json/issues/862
					jsonReader.DateParseHandling = DateParseHandling.None;

					JToken token = JToken.Load(jsonReader);
					switch (token.Type)
					{
						case JTokenType.Array:
							isBulkRequest = true;
							rpcRequests = ((JArray)token).Select(this.DeserializeRequest).ToList();
							break;
						case JTokenType.Object:
							isBulkRequest = false;
							RpcRequestParseResult result = this.DeserializeRequest(token);
							rpcRequests = new List<RpcRequestParseResult> { result };
							break;
						default:
							throw new RpcException(RpcErrorCode.ParseError, "Json body is not an array or an object.");
					}
				}
			}
			catch (Exception ex) when (!(ex is RpcException))
			{
				string errorMessage = "Unable to parse json request into an rpc format.";
				this.logger?.LogException(ex, errorMessage);
				throw new RpcException(RpcErrorCode.InvalidRequest, errorMessage, ex);
			}

			if (rpcRequests == null || !rpcRequests.Any())
			{
				throw new RpcException(RpcErrorCode.InvalidRequest, "No rpc json requests found");
			}
			this.logger?.LogDebug($"Successfully parsed {rpcRequests.Count} Rpc request(s)");
			var uniqueIds = new HashSet<RpcId>();
			foreach (RpcRequestParseResult result in rpcRequests.Where(r => r.Request != null && r.Request.Id.HasValue))
			{
				bool unique = uniqueIds.Add(result.Request.Id);
				if (!unique)
				{
					throw new RpcException(RpcErrorCode.InvalidRequest, "Duplicate ids in batch requests are not allowed");
				}
			}
			return ParsingResult.FromResults(rpcRequests, isBulkRequest);
		}
		
		private RpcRequestParseResult DeserializeRequest(JToken token)
		{
			RpcId id = null;
			JToken idToken = token[JsonRpcContants.IdPropertyName];
			if (idToken != null)
			{
				switch (idToken.Type)
				{
					case JTokenType.Null:
						break;
					case JTokenType.Integer:
					case JTokenType.Float:
						id = new RpcId(idToken.Value<double>());
						break;
					case JTokenType.String:
					case JTokenType.Guid:
						id = new RpcId(idToken.Value<string>());
						break;
					default:
						//Throw exception here because we need an id for the response
						throw new RpcException(RpcErrorCode.ParseError, "Unable to parse rpc id as string or number.");
				}
			}
			try
			{
				string rpcVersion = token.Value<string>(JsonRpcContants.VersionPropertyName);
				if (string.IsNullOrWhiteSpace(rpcVersion))
				{
					return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, "The jsonrpc version must be specified."));
				}
				if (!string.Equals(rpcVersion, "2.0", StringComparison.OrdinalIgnoreCase))
				{
					return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, $"The jsonrpc version '{rpcVersion}' is not supported. Supported versions: '2.0'"));
				}

				string method = token.Value<string>(JsonRpcContants.MethodPropertyName);
				if (string.IsNullOrWhiteSpace(method))
				{
					return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, "The request method is required."));
				}

				RpcParameters parameters = default;
				JToken paramsToken = token[JsonRpcContants.ParamsPropertyName];
				if (paramsToken != null)
				{
					switch (paramsToken.Type)
					{
						case JTokenType.Array:
							if (paramsToken.Any())
							{
								parameters = RpcParameters.FromList(paramsToken.ToArray());
							}
							break;
						case JTokenType.Object:
							if (paramsToken.Children().Any())
							{
								Dictionary<string, object> dict = paramsToken.ToObject<Dictionary<string, JToken>>()
									.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
								parameters = RpcParameters.FromDictionary(dict);
							}
							break;
						case JTokenType.Null:
							break;
						default:
							return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.ParseError, "Parameters field could not be parsed."));
					}
				}

				return RpcRequestParseResult.Success(new RpcRequest(id, method, parameters));
			}
			catch (Exception ex)
			{
				RpcError error;
				if (ex is RpcException rpcException)
				{
					error = rpcException.ToRpcError();
				}
				else
				{
					error = new RpcError(RpcErrorCode.ParseError, "Failed to parse request.", ex);
				}
				return RpcRequestParseResult.Fail(id, error);
			}
		}
	}
}
