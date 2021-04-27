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
			bool isBulkRequest = false;
			try
			{
				if (jsonStream.Length > int.MaxValue)
				{
					throw new RpcException(RpcErrorCode.ParseError, "Json body is too large to parse.");
				}
				var jsonDocument = JsonDocument.Parse(jsonStream);
				switch (jsonDocument.RootElement.ValueKind)
				{
					case JsonValueKind.Object:
						RpcRequestParseResult result = this.ParseResult(jsonDocument.RootElement.EnumerateObject());
						rpcRequests = new List<RpcRequestParseResult> { result };
						break;
					case JsonValueKind.Array:
						isBulkRequest = true;
						rpcRequests = new List<RpcRequestParseResult>();
						foreach(JsonElement element in jsonDocument.RootElement.EnumerateArray())
						{
							RpcRequestParseResult r = this.ParseResult(element.EnumerateObject());
							rpcRequests.Add(r);
						}
						break;
					default:
						throw new RpcException(RpcErrorCode.InvalidRequest, "Json request was invalid");
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

		private RpcRequestParseResult ParseResult(JsonElement.ObjectEnumerator objectEnumerator)
		{
			RpcId id = default;
			string? method = null;
			RpcParameters? parameters = null;
			string? rpcVersion = null;
			try
			{
				foreach (JsonProperty property in objectEnumerator)
				{
					switch (property.Name)
					{
						case JsonRpcContants.IdPropertyName:
							switch (property.Value.ValueKind)
							{
								case JsonValueKind.String:
									id = new RpcId(property.Value.GetString());
									break;
								case JsonValueKind.Number:
									if (!property.Value.TryGetInt64(out long longId))
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
							rpcVersion = property.Value.GetString();
							break;
						case JsonRpcContants.MethodPropertyName:
							method = property.Value.GetString();
							break;
						case JsonRpcContants.ParamsPropertyName:
							RpcParameters ps;
							switch (property.Value.ValueKind)
							{
								case JsonValueKind.Array:
									IRpcParameter[] items = property.Value
										.EnumerateArray()
										.Select(this.GetParameter)
										.Cast<IRpcParameter>()
										.ToArray();
									//TODO array vs list?
									ps = new RpcParameters(items);
									break;
								case JsonValueKind.Object:
									Dictionary<string, IRpcParameter> dict = property.Value
										.EnumerateObject()
										.ToDictionary(j => j.Name, j => (IRpcParameter)this.GetParameter(j.Value));
									ps = new RpcParameters(dict);
									break;
								default:
									return RpcRequestParseResult.Fail(id, new RpcError(RpcErrorCode.InvalidRequest, "The request parameter format is invalid."));
							}
							parameters = ps;
							break;
					}
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

		private JsonBytesRpcParameter GetParameter(JsonElement element)
		{
			RpcParameterType paramType = GetParameters(element);
			return new JsonBytesRpcParameter(paramType, element, this.serverConfig.Value.JsonSerializerSettings);
		}

		private static RpcParameterType GetParameters(JsonElement element)
		{
			return element.ValueKind switch
			{
				JsonValueKind.Number => RpcParameterType.Number,
				JsonValueKind.True or JsonValueKind.False => RpcParameterType.Boolean,
				JsonValueKind.Null or JsonValueKind.Undefined => RpcParameterType.Null,
				JsonValueKind.String => RpcParameterType.String,
				JsonValueKind.Object => RpcParameterType.Object,
				JsonValueKind.Array => RpcParameterType.Array,
				_ => throw new NotImplementedException($"Uninplemented json value type: '{element.ValueKind}'"),
			};
		}
	}

	internal class JsonBytesSequenceSegment : ReadOnlySequenceSegment<byte>
	{
		public JsonBytesSequenceSegment(Memory<byte> bytes, JsonBytesSequenceSegment? next = null)
		{
			this.Memory = bytes;
			this.Next = next;
		}
	}
}
