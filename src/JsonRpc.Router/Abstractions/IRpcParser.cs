using System.Collections.Generic;

namespace JsonRpc.Router.Abstractions
{
	public interface IRpcParser
	{
		bool MatchesRpcRoute(string requestUrl, out string section);
		List<RpcRequest> ParseRequests(string jsonString);
	}
}
