using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcInvoker
	{
		/// <summary>
		/// Call the incoming Rpc request method and gives the appropriate response
		/// </summary>
		/// <param name="request">Rpc request</param>
		/// <returns>An Rpc response for the request</returns>
		Task<RpcResponse?> InvokeRequestAsync(RpcRequest request);

		/// <summary>
		/// Call the incoming Rpc requests methods and gives the appropriate respones
		/// </summary>
		/// <param name="requests">List of Rpc requests to invoke</param>
		/// <returns>List of Rpc responses for the requests</returns>
		Task<List<RpcResponse>> InvokeBatchRequestAsync(IList<RpcRequest> requests);
	}
}
