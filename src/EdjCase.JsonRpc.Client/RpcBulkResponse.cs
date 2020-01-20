using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdjCase.JsonRpc.Common;

namespace EdjCase.JsonRpc.Client
{
	public class RpcBulkResponse
	{
		private IDictionary<RpcId, RpcResponse> responses { get; }

		public RpcBulkResponse(IDictionary<RpcId, RpcResponse> responses)
		{
			this.responses = responses ?? throw new ArgumentNullException(nameof(responses));
		}

		public RpcResponse GetResponse(RpcId id)
		{
			return this.responses[id];
		}

		public RpcResponse<T> GetResponse<T>(RpcId id)
		{
			return RpcResponse<T>.FromResponse(this.responses[id]);
		}

		public List<RpcResponse> GetResponses()
		{
			return this.responses
				.Select(r => r.Value)
				.ToList();
		}

		public List<RpcResponse<T>> GetResponses<T>()
		{
			return this.responses
				.Select(r => RpcResponse<T>.FromResponse(r.Value))
				.ToList();
		}

		internal static RpcBulkResponse FromResponses(List<RpcResponse> responses)
		{
			Dictionary<RpcId, RpcResponse> responseMap = responses.ToDictionary(r => r.Id, r => r);
			return new RpcBulkResponse(responseMap);
		}
	}
}
