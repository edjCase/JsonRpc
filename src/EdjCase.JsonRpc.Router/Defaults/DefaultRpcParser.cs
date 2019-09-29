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
using System.Text.Json;
using System.Buffers;
using System.Buffers.Text;

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
			this.logger.LogDebug($"Attempting to parse Rpc request from the json");
			List<RpcRequestParseResult> rpcRequests = null;
			if (jsonStream == null || jsonStream.Length < 1)
			{
				throw new RpcException(RpcErrorCode.InvalidRequest, "Json request was empty");
			}
			bool isBulkRequest = false;
			try
			{
				if (jsonStream.Length > int.MaxValue)
				{
					throw new RpcException(RpcErrorCode.ParseError, "Json body is too large to parse.");
				}
				byte[] jsonBytes = ArrayPool<byte>.Shared.Rent((int)jsonStream.Length);
				try
				{
					jsonStream.Read(jsonBytes, 0, (int)jsonStream.Length);
					var jsonReader = new System.Text.Json.Utf8JsonReader(jsonBytes);
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
				finally
				{
					ArrayPool<byte>.Shared.Return(jsonBytes, clearArray: false);
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
			this.logger.LogDebug($"Successfully parsed {rpcRequests.Count} Rpc request(s)");
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

		private RpcRequestParseResult ParseResult(ref Utf8JsonReader jsonReader)
		{
			RpcId id = default;
			string method = null;
			RpcParameters parameters = null;
			string rpcVersion = null;
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
							RpcParameters ps;
							switch (jsonReader.TokenType)
							{
								case JsonTokenType.StartArray:
									jsonReader.Read();
									var list = new List<IRpcParameter>();
									while (jsonReader.TokenType != JsonTokenType.EndArray)
									{
										IRpcParameter parameter = this.GetParameter(ref jsonReader);
										list.Add(parameter);
									}
									ps = new RpcParameters(list);
									break;
								case JsonTokenType.StartObject:
									jsonReader.Read();
									var dict = new Dictionary<string, IRpcParameter>();
									while (jsonReader.TokenType != JsonTokenType.EndObject)
									{
										string key = jsonReader.GetString();
										jsonReader.Read();
										IRpcParameter parameter = this.GetParameter(ref jsonReader);
										dict.Add(key, parameter);
									}
									ps = new RpcParameters(dict);
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

				return RpcRequestParseResult.Success(id, method, parameters);
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

		private JsonBytesRpcParameter GetParameter(ref Utf8JsonReader jsonReader)
		{
			//TODO shared memory?
			byte[] bytes = jsonReader.HasValueSequence
				? jsonReader.ValueSequence.ToArray()
				: jsonReader.ValueSpan.ToArray();

			RpcParameterType paramType;
			switch (jsonReader.TokenType)
			{
				case JsonTokenType.Number:
					paramType = RpcParameterType.Number;
					break;
				case JsonTokenType.Null:
					paramType = RpcParameterType.Null;
					break;
				case JsonTokenType.String:
					paramType = RpcParameterType.String;
					break;
				case JsonTokenType.StartObject:
					paramType = RpcParameterType.Object;
					break;
				default:
					throw new RpcException(RpcErrorCode.ParseError, "Invalid json");
			}
            jsonReader.Read();
			return new JsonBytesRpcParameter(paramType, bytes, this.serverConfig.Value?.JsonSerializerSettings);
		}
	}
}
