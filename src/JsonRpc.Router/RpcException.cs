using System;

namespace edjCase.JsonRpc.Router
{
	/// <summary>
	/// Base Rpc exception that contains Rpc specfic error info
	/// </summary>
	public abstract class RpcException : Exception
	{
		/// <summary>
		/// Rpc error code that corresponds to the documented integer codes
		/// </summary>
		public RpcErrorCode ErrorCode { get; }
		/// <summary>
		/// Custom data attached to the error if needed
		/// </summary>
		public object RpcData { get; }
		
		/// <param name="errorCode">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Custom data if needed for error response</param>
		protected RpcException(RpcErrorCode errorCode, string message, object data = null) : base(message)
		{
			this.ErrorCode = errorCode;
			this.RpcData = data;
		}
	}

	/// <summary>
	/// Exception for invalid request formats or malformed requests
	/// </summary>
	public class RpcInvalidRequestException : RpcException
	{
		/// <param name="message">Error message</param>
		public RpcInvalidRequestException(string message) : base(RpcErrorCode.InvalidRequest, message)
		{
		}
	}

	/// <summary>
	/// Exception for requests that match multiple methods for invoking
	/// </summary>
	public class RpcAmbiguousMethodException : RpcException
	{
		public RpcAmbiguousMethodException() : base(RpcErrorCode.AmbiguousMethod, "Request matches multiple method signatures")
		{
		}
	}

	/// <summary>
	/// Exception for requests that match no methods for invoking
	/// </summary>
	public class RpcMethodNotFoundException : RpcException
	{
		public RpcMethodNotFoundException() : base(RpcErrorCode.MethodNotFound, "No method found with the requested signature")
		{
		}
	}

	/// <summary>
	/// Exception for requests that match a method but have invalid parameters
	/// </summary>
	public class RpcInvalidParametersException : RpcException
	{
		public RpcInvalidParametersException() : base(RpcErrorCode.InvalidParams, "Parameters do not match")
		{
		}
	}

	/// <summary>
	/// Exception for requests that have an unexpected or unknown exception thrown
	/// </summary>
	public class RpcUnknownException : RpcException
	{
		/// <param name="message">Error message</param>
		public RpcUnknownException(string message) : base(RpcErrorCode.InternalError, message)
		{
		}
	}



	/// <summary>
	/// Exception for bad configuration of the Rpc server
	/// </summary>
	//Not a request exception so it is not an `RpcException`
	public class RpcConfigurationException : Exception
	{
		/// <param name="message">Error message</param>
		public RpcConfigurationException(string message) : base(message)
		{
		}
	}
}
