using System.Collections.Generic;

namespace edjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcInvoker
	{
		/// <summary>
		/// Call the incoming Rpc request method and gives the appropriate response
		/// </summary>
		/// <param name="request">Rpc request</param>
		/// <param name="route">Rpc route that applies to the current request</param>
		/// <returns>An Rpc response for the request</returns>
		RpcResponseBase InvokeRequest(RpcRequest request, RpcRoute route);

		/// <summary>
		/// Call the incoming Rpc requests methods and gives the appropriate respones
		/// </summary>
		/// <param name="request">List of Rpc requests</param>
		/// <param name="route">Rpc route that applies to the current request</param>
		/// <returns>List of Rpc responses for the requests</returns>
		List<RpcResponseBase> InvokeBatchRequest(List<RpcRequest> requests, RpcRoute route);
	}
}
