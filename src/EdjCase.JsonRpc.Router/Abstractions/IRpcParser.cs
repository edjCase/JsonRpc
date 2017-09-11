using System.Collections.Generic;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcParser
	{
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
