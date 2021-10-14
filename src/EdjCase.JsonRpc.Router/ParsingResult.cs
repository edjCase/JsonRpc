using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Utilities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router
{
	public class ParsingResult
	{
		/// <summary>
		/// Successfully parsed request
		/// </summary>
		public List<RpcRequest> Requests { get; }
		/// <summary>
		/// Errors with the associated request id
		/// </summary>
		public List<(RpcId Id, RpcError Error)> Errors { get; }
		/// <summary>
		/// Flag to indicate if the request was an array vs singular
		/// </summary>
		public bool IsBulkRequest { get; }
		/// <summary>
		/// Count of total requests processed (successful and failed)
		/// </summary>
		public int RequestCount => this.Requests.Count + this.Errors.Count;

		public ParsingResult(List<RpcRequest> requests, List<(RpcId, RpcError)> errors, bool isBulkRequest)
		{
			this.Requests = requests;
			this.Errors = errors;
			this.IsBulkRequest = isBulkRequest;
		}

		internal static ParsingResult FromResults(List<RpcRequestParseResult> results, bool isBulkRequest)
		{
			var requests = new List<RpcRequest>();
			var errors = new List<(RpcId, RpcError)>();
			foreach (RpcRequestParseResult result in results)
			{
				if (result.Error != null)
				{
					errors.Add((result.Id, result.Error));
				}
				else
				{
					requests.Add(new RpcRequest(result.Id, result.Method!, result.Parameters));
				}
			}
			//safety check
			isBulkRequest = isBulkRequest || (requests.Count + errors.Count > 1);
			return new ParsingResult(requests, errors, isBulkRequest);
		}
	}

	internal class RpcRequestParseResult
	{
		public RpcId Id { get; }
		public string? Method { get; }
		public TopLevelRpcParameters? Parameters { get; }
		public RpcError? Error { get; }
		private RpcRequestParseResult(RpcId id, string? method, TopLevelRpcParameters? parameters, RpcError? error)
		{
			this.Id = id;
			this.Method = method;
			this.Parameters = parameters;
			this.Error = error;
		}

		public static RpcRequestParseResult Success(RpcId id, string method, TopLevelRpcParameters? parameters)
		{
			return new RpcRequestParseResult(id, method, parameters, null);
		}

		public static RpcRequestParseResult Fail(RpcId id, RpcError error)
		{
			return new RpcRequestParseResult(id, null, null, error);
		}
	}
}
