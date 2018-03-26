using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Defaults
{
	/// <summary>
	/// Error result for rpc responses
	/// </summary>
	public class RpcMethodErrorResult : IRpcMethodResult
	{
		/// <summary>
		/// Error message
		/// </summary>
		public string Message { get; }
		/// <summary>
		/// JSON-RPC error code
		/// </summary>
		public int ErrorCode { get; }

		/// <summary>
		/// Data for error response
		/// </summary>
		public object Data { get; }

		/// <summary>
		/// Server exception that will only be shown in the response if configured
		/// </summary>
		public Exception Exception { get; }

		/// <param name="errorCode">JSON-RPC error code</param>
		/// <param name="message">(Optional)Error message</param>
		/// <param name="data">(Optional)Data for error response</param>
		public RpcMethodErrorResult(int errorCode, string message = null, Exception serverException = null, object data = null)
		{
			this.ErrorCode = errorCode;
			this.Message = message;
			this.Data = data;
		}

		/// <summary>
		/// Turns result data into a rpc response
		/// </summary>
		/// <param name="id">Rpc request id</param>
		/// <param name="serializer">Json serializer function to use for objects for the response</param>
		/// <returns>Rpc response for request</returns>
		public RpcResponse ToRpcResponse(RpcId id)
		{
			RpcError error = new RpcError(this.ErrorCode, this.Message, this.Exception, this.Data);
			return new RpcResponse(id, error);
		}
	}

	/// <summary>
	/// Success result for rpc responses
	/// </summary>
	public class RpcMethodSuccessResult : IRpcMethodResult
	{
		/// <summary>
		/// Object to return in rpc response
		/// </summary>
		public object ReturnObject { get; }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="returnObject">Object to return in rpc response</param>
		public RpcMethodSuccessResult(object returnObject = null)
		{
			this.ReturnObject = returnObject;
		}

		/// <summary>
		/// Turns result data into a rpc response
		/// </summary>
		/// <param name="id">Rpc request id</param>
		/// <param name="serializer">Json serializer function to use for objects for the response</param>
		/// <returns>Rpc response for request</returns>
		public RpcResponse ToRpcResponse(RpcId id)
		{
			return new RpcResponse(id, this.ReturnObject);
		}
	}
}
