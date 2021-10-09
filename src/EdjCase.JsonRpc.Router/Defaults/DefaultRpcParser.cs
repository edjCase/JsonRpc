using System;
using System.Collections.Generic;
using System.Linq;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Options;
using System.IO;
using EdjCase.JsonRpc.Router;
using System.Text.Json;
using System.Buffers;
using System.Buffers.Text;

namespace EdjCase.JsonRpc.Router.Defaults
{
	/// <summary>
	/// Default Rpc parser that uses <see cref="Newtonsoft.Json"/>
	/// </summary>
	internal class DefaultRpcParser : IRpcParser
	{
		/// <summary>
		/// Logger for logging Rpc parsing
		/// </summary>
		private ILogger<DefaultRpcParser> logger { get; }
		private IOptions<RpcServerConfiguration> serverConfig { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger">Optional logger for logging Rpc parsing</param>
		public DefaultRpcParser(ILogger<DefaultRpcParser> logger,
			IOptions<RpcServerConfiguration> serverConfig)
		{
			this.logger = logger;
			this.serverConfig = serverConfig;
		}

		/// <summary>
		/// Parses all the requests from the json in the request
		/// </summary>
		/// <param name="jsonString">Json from the http request</param>
		/// <param name="isBulkRequest">If true, the request is a bulk request (even if there is only one)</param>
		/// <returns>List of Rpc requests that were parsed from the json</returns>
		public ParsingResult ParseRequests(Stream jsonStream)
		{
			this.logger.ParsingRequests();
			List<RpcRequestParseResult>? rpcRequests = null;

			if (jsonStream == null || jsonStream.Length < 1)
			{
				throw new RpcException(RpcErrorCode.InvalidRequest, "Json request was empty");
			}

			var jsonReader = new Utf8JsonStreamReader(jsonStream);

			bool isBulkRequest = false;
			try
			{
				if (jsonReader.Read())
				{
					switch (jsonReader.TokenType)
					{
						case JsonTokenType.StartObject:
							jsonReader.Read();
							RpcRequestParseResult result = this.ParseResult(ref jsonReader);
							rpcRequests = new List<RpcRequestParseResult> { result };
							break;
						case JsonTokenType.StartArray:
							isBulkRequest = true;
							jsonReader.Read();
							rpcRequests = new List<RpcRequestParseResult>();
							while (jsonReader.TokenType != JsonTokenType.EndArray)
							{
								RpcRequestParseResult r = this.ParseResult(ref jsonReader);
								rpcRequests.Add(r);
								jsonReader.Read();
							}
							break;
						default:
							throw new RpcException(RpcErrorCode.InvalidRequest, "Json request was invalid");
					}

				}

			}
			catch (Exception ex) when (!(ex is RpcException))
			{
				string errorMessage = "Unable to parse json request into an rpc format.";
				this.logger.LogException(ex, errorMessage);
				throw new RpcException(RpcErrorCode.InvalidRequest, errorMessage, ex);
			}

			if (rpcRequests == null || !rpcRequests.Any())
			{
				throw new RpcException(RpcErrorCode.InvalidRequest, "No rpc json requests found");
			}
			this.logger.ParsedRequests(rpcRequests.Count);
			var uniqueIds = new HashSet<RpcId>();
			foreach (RpcRequestParseResult result in rpcRequests.Where(r => r.Id.HasValue))
			{
				bool unique = uniqueIds.Add(result.Id);
				if (!unique)
				{
					throw new RpcException(RpcErrorCode.InvalidRequest, "Duplicate ids in batch requests are not allowed");
				}
			}
			return ParsingResult.FromResults(rpcRequests, isBulkRequest);
		}


		public RpcRequestParseResult ParseResult(ref Utf8JsonStreamReader jsonReader)
		{
			RpcId id = default;
			string? method = null;
			TopLevelRpcParameters? parameters = null;
			string? rpcVersion = null;
			try
			{
				if (jsonReader.TokenType == JsonTokenType.StartObject)
				{
					jsonReader.Read();
				}
				while (jsonReader.TokenType != JsonTokenType.EndObject)
				{
					string propertyName = jsonReader.GetString();
					jsonReader.Read();
					switch (propertyName)
					{
						case JsonRpcContants.IdPropertyName:
							switch (jsonReader.TokenType)
							{
								case JsonTokenType.String:
									id = new RpcId(jsonReader.GetString());
									break;
								case JsonTokenType.Number:
									if (!jsonReader.TryGetInt64(out long longId))
									{
										var idError = new RpcError(RpcErrorCode.ParseError, "Unable to parse rpc id as an integer");
										return RpcRequestParseResult.Fail(id, idError);
									}
									id = new RpcId(longId);
									break;
								default:
									var error = new RpcError(RpcErrorCode.ParseError, "Unable to parse rpc id as a string or an integer");
									return RpcRequestParseResult.Fail(id, error);
							}
							break;
						case JsonRpcContants.VersionPropertyName:
							rpcVersion = jsonReader.GetString();
							break;
						case JsonRpcContants.MethodPropertyName:
							method = jsonReader.GetString();
							break;
						case JsonRpcContants.ParamsPropertyName:
							TopLevelRpcParameters ps;
							switch (jsonReader.TokenType)
							{
								case JsonTokenType.StartArray:
									RpcParameter[] array = this.ParseArray(ref jsonReader);
									ps = new TopLevelRpcParameters(array);
									break;
								case JsonTokenType.StartObject:
									Dictionary<string, RpcParameter> dict = this.ParseObject(ref jsonReader);
									ps = new TopLevelRpcParameters(dict);
									break;
								default:
									return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, "The request parameter format is invalid."));
							}
							parameters = ps;
							break;
					}
					jsonReader.Read();
				}

				if (string.IsNullOrWhiteSpace(method))
				{
					return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, "The request method is required."));
				}
				if (string.IsNullOrWhiteSpace(rpcVersion))
				{
					return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, "The jsonrpc version must be specified."));
				}
				if (!string.Equals(rpcVersion, "2.0", StringComparison.OrdinalIgnoreCase))
				{
					return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, $"The jsonrpc version '{rpcVersion}' is not supported. Supported versions: '2.0'"));
				}

				return RpcRequestParseResult.Success(id, method!, parameters);
			}
			catch (Exception ex)
			{
				RpcError error;
				if (ex is RpcException rpcException)
				{
					error = rpcException.ToRpcError(this.serverConfig.Value.ShowServerExceptions);
				}
				else
				{
					error = new RpcError(RpcErrorCode.ParseError, "Failed to parse request.", ex);
				}
				return RpcRequestParseResult.Fail(id, error);
			}
		}

		private RpcParameter GetParameter(ref Utf8JsonStreamReader jsonReader)
		{
			RpcParameter parameter;
			switch (jsonReader.TokenType)
			{
				case JsonTokenType.Number:
					decimal d = jsonReader.GetDecimal();
					var number = new RpcNumber(d.ToString());
					parameter = RpcParameter.Number(number);
					break;
				case JsonTokenType.True:
				case JsonTokenType.False:
					parameter = RpcParameter.Boolean(jsonReader.GetBoolean());
					break;
				case JsonTokenType.Null:
					parameter = RpcParameter.Null();
					break;
				case JsonTokenType.String:
					parameter = RpcParameter.String(jsonReader.GetString());
					break;
				case JsonTokenType.StartObject:
					Dictionary<string, RpcParameter> obj = this.ParseObject(ref jsonReader);
					parameter = RpcParameter.Object(obj);
					break;
				case JsonTokenType.StartArray:
					RpcParameter[] array = this.ParseArray(ref jsonReader);
					parameter = RpcParameter.Array(array);
					break;
				default:
					throw new RpcException(RpcErrorCode.ParseError, "Invalid json");
			}
			jsonReader.Read();
			return parameter;
		}

		private RpcParameter[] ParseArray(ref Utf8JsonStreamReader jsonReader)
		{
			jsonReader.Read();
			var list = new List<RpcParameter>();
			while (jsonReader.TokenType != JsonTokenType.EndArray)
			{
				RpcParameter parameter = this.GetParameter(ref jsonReader);
				list.Add(parameter);
			}
			return list.ToArray();
		}

		private Dictionary<string, RpcParameter> ParseObject(ref Utf8JsonStreamReader jsonReader)
		{
			jsonReader.Read();
			var dict = new Dictionary<string, RpcParameter>();
			while (jsonReader.TokenType != JsonTokenType.EndObject)
			{
				string key = jsonReader.GetString();
				jsonReader.Read();
				RpcParameter parameter = this.GetParameter(ref jsonReader);
				dict.Add(key, parameter);
			}
			return dict;
		}
	}
}
