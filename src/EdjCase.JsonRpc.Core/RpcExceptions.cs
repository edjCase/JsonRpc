using Newtonsoft.Json.Linq;
using System;

namespace EdjCase.JsonRpc.Core
{
	/// <summary>
	/// Base Rpc server exception that contains Rpc specfic error info
	/// </summary>
	public abstract class RpcException : Exception
	{
		/// <summary>
		/// Rpc error code that corresponds to the documented integer codes
		/// </summary>
		public int ErrorCode { get; }
		/// <summary>
		/// Custom data attached to the error if needed
		/// </summary>
		public JToken RpcData { get; }

		/// <param name="errorCode">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Custom data if needed for error response</param>
		/// <param name="innerException">Inner exception (optional)</param>
		protected RpcException(int errorCode, string message, JToken data = null, Exception innerException = null) : base(message, innerException)
		{
			this.ErrorCode = errorCode;
			this.RpcData = data;
		}
		
		/// <param name="error">Rpc error to make into an exception</param>
		protected RpcException(RpcError error) : this(error.Code, error.Message, error.Data)
		{
			
		}
	}

	/// <summary>
	/// Exception for invalid request formats or malformed requests
	/// </summary>
	public class RpcInvalidRequestException : RpcException
	{
		internal RpcInvalidRequestException(RpcError error) : base(error)
		{
		}

		/// <param name="message">Error message</param>
		public RpcInvalidRequestException(string message) : base((int)RpcErrorCode.InvalidRequest, message)
		{
		}
	}
	
	/// <summary>
	/// Exception for requests that match no methods for invoking
	/// </summary>
	public class RpcMethodNotFoundException : RpcException
	{
		internal RpcMethodNotFoundException(RpcError error) : base(error)
		{
		}
		public RpcMethodNotFoundException() : base((int)RpcErrorCode.MethodNotFound, "No method found with the requested signature or multiple methods matched the request.")
		{
		}
	}

	/// <summary>
	/// Exception for requests that match a method but have invalid parameters
	/// </summary>
	public class RpcInvalidParametersException : RpcException
	{
		internal RpcInvalidParametersException(RpcError error) : base(error)
		{
		}
		public RpcInvalidParametersException(string message, Exception innerException = null) : base((int)RpcErrorCode.InvalidParams, message, null, innerException)
		{
		}
	}

	/// <summary>
	/// Exception for requests that have an unexpected or unknown exception thrown
	/// </summary>
	public class RpcUnknownException : RpcException
	{
		internal RpcUnknownException(RpcError error) : base(error)
		{
		}

		/// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception (optional)</param>
		public RpcUnknownException(string message, Exception innerException = null) : base((int)RpcErrorCode.InternalError, message, null, innerException)
		{
		}
	}

	/// <summary>
	/// Exception for requests that have parsing error
	/// </summary>
	public class RpcParseException : RpcException
	{
		internal RpcParseException(RpcError error) : base(error)
		{
		}

		/// <param name="message">Error message</param>
		public RpcParseException(string message) : base((int)RpcErrorCode.ParseError, message)
		{
		}
	}


	/// <summary>
	/// Custom exception defined by the server
	/// </summary>
	public class RpcCustomException : RpcException
	{
		internal RpcCustomException(RpcError error) : base(error)
		{
		}

		public RpcCustomException(int code, string message, Exception innerException = null) : base(code, message, null, innerException)
		{
		}
	}
}
