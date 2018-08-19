using EdjCase.JsonRpc.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	/// <summary>
	/// Rpc method response object that allows the server to customize an rpc response
	/// </summary>
	public interface IRpcMethodResult
	{
		/// <summary>
		/// Turns result data into a rpc response
		/// </summary>
		/// <param name="id">Rpc request id</param>
		/// <param name="serializer">Json serializer function to use for objects for the response</param>
		/// <returns>Rpc response for request</returns>
		RpcResponse ToRpcResponse(RpcId id);
	}
}
