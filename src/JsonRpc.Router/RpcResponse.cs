using System;
using edjCase.JsonRpc.Router.JsonConverters;
using Newtonsoft.Json;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace edjCase.JsonRpc.Router
{
	/// <summary>
	/// Model representing an Rpc error response
	/// </summary>
	[JsonObject]
	public class RpcErrorResponse : RpcResponseBase
	{
		[JsonConstructor]
		private RpcErrorResponse()
		{

		}
		
		/// <param name="id">Request id</param>
		/// <param name="error">Request error</param>
		public RpcErrorResponse(object id, RpcError error) : base(id)
		{
			this.Error = error;
		}
		/// <summary>
		/// Error from processing Rpc request (Required)
		/// </summary>
		[JsonProperty("error", Required = Required.Always)]
		public RpcError Error { get; private set; }
	}

	/// <summary>
	/// Model representing a successful Rpc response
	/// </summary>
	[JsonObject]
	public class RpcResultResponse : RpcResponseBase
	{
		[JsonConstructor]
		private RpcResultResponse()
		{

		}

		/// <param name="id">Request id</param>
		/// <param name="result">Response result object</param>
		public RpcResultResponse(object id, object result) : base(id)
		{
			this.Result = result;
		}

		/// <summary>
		/// Reponse result object (Required)
		/// </summary>
		[JsonProperty("result", Required = Required.Always)]
		public object Result { get; private set; }
	}
	
	/// <summary>
	/// Base class for all Rpc responses
	/// </summary>
	[JsonObject]
	public abstract class RpcResponseBase
	{
		[JsonConstructor]
		protected RpcResponseBase()
		{

		}
		
		/// <param name="id">Request id</param>
		protected RpcResponseBase(object id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Request id (Required but nullable)
		/// </summary>
		[JsonProperty("id", Required = Required.AllowNull)]
		[JsonConverter(typeof(RpcIdJsonConverter))]
		public object Id { get; private set; }

		/// <summary>
		/// Rpc request version (Required)
		/// </summary>
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpcVersion { get; private set; } = "2.0";
	}

	/// <summary>
	/// Model to represent an Rpc response error
	/// </summary>
	[JsonObject]
	public class RpcError
	{
		[JsonConstructor]
		private RpcError()
		{

		}
		
		/// <param name="exception">Exception from Rpc request</param>
		public RpcError(RpcException exception)
		{
			if(exception == null)
			{
				throw new ArgumentNullException(nameof(exception));
			}
			this.Code = (int)exception.ErrorCode;
			this.Message = exception.Message;
			this.Data = exception.RpcData;
		}
		
		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(int code, string message, object data = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}
			this.Code = code;
			this.Message = message;
			this.Data = data;
		}
		/// <summary>
		/// Rpc error code (Required)
		/// </summary>
		[JsonProperty("code", Required = Required.Always)]
		public int Code { get; private set; }
		/// <summary>
		/// Error message (Required)
		/// </summary>
		[JsonProperty("message", Required = Required.Always)]
		public string Message { get; private set; }
		/// <summary>
		/// Error data (Optional)
		/// </summary>
		[JsonProperty("data")]
		public object Data { get; private set; }
	}
}
