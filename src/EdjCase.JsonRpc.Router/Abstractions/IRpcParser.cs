using System.Collections.Generic;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcParser
	{
		/// <summary>
		/// Indicates if the incoming request matches any predefined routes
		/// </summary>
		/// <param name="requestUrl">The current request url</param>
		/// <param name="route">The matching route corresponding to the request url if found, otherwise it is null</param>
		/// <param name="routeProvider">Provider that allows the retrieval of all configured routes</param>
		/// <returns>True if the request url matches any Rpc routes, otherwise False</returns>
		bool MatchesRpcRoute(IRpcRouteProvider routeProvider, string requestUrl, out RpcRoute route);

		/// <summary>
		/// Parses all the requests from the json in the request
		/// </summary>
		/// <param name="jsonString">Json from the http request</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <param name="isBulkRequest">If true, the request is a bulk request (even if there is only one)</param>
		/// <returns>List of Rpc requests that were parsed from the json</returns>
		List<RpcRequest> ParseRequests(string jsonString, out bool isBulkRequest, JsonSerializerSettings jsonSerializerSettings = null);
	}
}
