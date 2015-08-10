using System.Collections.Generic;

namespace JsonRpc.Router.Abstractions
{
	public interface IRpcInvoker
	{
		RpcResponseBase InvokeRequest(RpcRequest request, string section);
		List<RpcResponseBase> InvokeBatchRequest(List<RpcRequest> requests, string section);
	}
}
