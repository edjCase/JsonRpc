using System;
using System.Collections.Generic;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcInvoker
	{
		/// <summary>
		/// Call the incoming Rpc request method and gives the appropriate response
		/// </summary>
		/// <param name="request">Rpc request</param>
		/// <param name="path">Rpc path that applies to the current request</param>
		/// <param name="routeContext">The context of the current rpc request</param>
		/// <returns>An Rpc response for the request</returns>
		Task<RpcResponse> InvokeRequestAsync(RpcRequest request, RpcPath path, IRouteContext routeContext);

		/// <summary>
		/// Call the incoming Rpc requests methods and gives the appropriate respones
		/// </summary>
		/// <param name="requests">List of Rpc requests to invoke</param>
		/// <param name="path">Rpc route that applies to the current request</param>
		/// <param name="routeContext">The context of the current rpc request</param>
		/// <returns>List of Rpc responses for the requests</returns>
		Task<List<RpcResponse>> InvokeBatchRequestAsync(IList<RpcRequest> requests, RpcPath path, IRouteContext routeContext);
	}
}
