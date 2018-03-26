using EdjCase.JsonRpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Edjcase.JsonRpc.Router
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
		public int RequestCount { get; }

		public ParsingResult(List<RpcRequest> requests, List<(RpcId, RpcError)> errors, bool isBulkRequest)
		{
			this.Requests = requests;
			this.Errors = errors;
			this.IsBulkRequest = isBulkRequest;
			this.RequestCount = requests.Count + errors.Count;
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
					requests.Add(result.Request);
				}
			}
			return new ParsingResult(requests, errors, isBulkRequest);
		}
	}

	internal class RpcRequestParseResult
	{
		public RpcId Id { get; }
		public RpcRequest Request { get; }
		public RpcError Error { get; }
		private RpcRequestParseResult(RpcId id, RpcRequest request, RpcError error)
		{
			this.Id = id;
			this.Request = request;
			this.Error = error;
		}

		public static RpcRequestParseResult Success(RpcRequest request)
		{
			return new RpcRequestParseResult(request.Id, request, null);
		}

		public static RpcRequestParseResult Fail(RpcId id, RpcError error)
		{
			return new RpcRequestParseResult(id, null, error);
		}
	}
}
