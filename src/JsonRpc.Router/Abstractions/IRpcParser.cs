using System.Collections.Generic;

namespace edjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcParser
	{
		bool MatchesRpcRoute(RpcRouteCollection routes, string requestUrl, out RpcRoute route);
		List<RpcRequest> ParseRequests(string jsonString);
	}
}
