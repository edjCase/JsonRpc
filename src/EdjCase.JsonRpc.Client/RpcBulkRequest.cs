using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdjCase.JsonRpc.Common;

namespace EdjCase.JsonRpc.Client
{
	public class RpcBulkRequest
	{
		private IList<(RpcRequest Request, Type ResponseType)> requests { get; }
		public RpcBulkRequest(IList<(RpcRequest, Type)> requests)
		{
			if(requests == null)
			{
				throw new ArgumentNullException(nameof(requests));
			}
			if (!requests.Any())
			{
				throw new ArgumentException("Need at least one request", nameof(requests));
			}
			this.requests = requests;
		}

		public static RpcBulkRequest FromRequests(List<RpcRequest> requests, IDictionary<RpcId, Type> responseTypeMap)
		{
			List<(RpcRequest Request, Type ResponseType)> list = requests
				.Select(r => (r, responseTypeMap[r.Id]))
				.ToList();
			return new RpcBulkRequest(list);
		}

		public static RpcBulkRequest FromRequests<TResponseType>(List<RpcRequest> requests)
		{
			Type responseType = typeof(TResponseType);
			List<(RpcRequest Request, Type ResponseType)> list = requests
				.Select(r => (r, responseType))
				.ToList();
			return new RpcBulkRequest(list);
		}

		internal IDictionary<RpcId, Type> GetTypeMap()
		{
			return this.requests.ToDictionary(r => r.Request.Id, r => r.ResponseType);
		}

		internal List<RpcRequest> GetRequests()
		{
			return this.requests
				.Select(r => r.Request)
				.ToList();
		}
	}
}
