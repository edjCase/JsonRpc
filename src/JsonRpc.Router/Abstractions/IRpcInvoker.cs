using System;
using System.Collections.Generic;
using edjCase.JsonRpc.Core;

namespace edjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcInvoker
	{
		/// <summary>
		/// Call the incoming Rpc request method and gives the appropriate response
		/// </summary>
		/// <param name="request">Rpc request</param>
		/// <param name="route">Rpc route that applies to the current request</param>
		/// <param name="serviceProvider">(Optional)IoC Container for rpc method controllers</param>
		/// <returns>An Rpc response for the request</returns>
		RpcResponse InvokeRequest(RpcRequest request, RpcRoute route, IServiceProvider serviceProvider = null);

		/// <summary>
		/// Call the incoming Rpc requests methods and gives the appropriate respones
		/// </summary>
		/// <param name="requests">List of Rpc requests to invoke</param>
		/// <param name="route">Rpc route that applies to the current request</param>
		/// <param name="serviceProvider">(Optional)IoC Container for rpc method controllers</param>
		/// <returns>List of Rpc responses for the requests</returns>
		List<RpcResponse> InvokeBatchRequest(List<RpcRequest> requests, RpcRoute route, IServiceProvider serviceProvider = null);
	}
}
