using System.Collections.Generic;

namespace JsonRpc.Router.Abstractions
{
	public interface IRpcParser
	{
		bool MatchesRpcRoute(string requestUrl, out RpcRoute route);
		List<RpcRequest> ParseRequests(string jsonString);
	}
}
