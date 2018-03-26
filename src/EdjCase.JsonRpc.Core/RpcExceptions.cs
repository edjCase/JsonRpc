using System;

namespace EdjCase.JsonRpc.Core
{
	/// <summary>
	/// Rpc server exception that contains Rpc specfic error info
	/// </summary>
	public class RpcException : Exception
	{
		/// <summary>
		/// Rpc error code that corresponds to the documented integer codes
		/// </summary>
		public int ErrorCode { get; }
		/// <summary>
		/// Custom data attached to the error if needed
		/// </summary>
		public object RpcData { get; }
		
		/// <param name="errorCode">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Custom data if needed for error response</param>
		/// <param name="innerException">Inner exception (optional)</param>
		public RpcException(int errorCode, string message, Exception innerException = null, object data = null) : base(message, innerException)
		{
			this.ErrorCode = errorCode;
			this.RpcData = data;
		}
		/// <param name="errorCode">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Custom data if needed for error response</param>
		/// <param name="innerException">Inner exception (optional)</param>
		public RpcException(RpcErrorCode errorCode, string message, Exception innerException = null, object data = null) : this((int)errorCode, message, innerException, data)
		{
		}

		public RpcError ToRpcError()
		{
			return new RpcError(this.ErrorCode, this.Message, this.InnerException, this.RpcData);
		}
	}
	
}
