using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public abstract class RpcException : Exception
	{
		public RpcErrorCode ErrorCode { get; }
		public object RpcData { get; }

		protected RpcException(RpcErrorCode errorCode, string message, object data = null) : base(message)
		{
			this.ErrorCode = errorCode;
			this.RpcData = data;
		}
	}
	public class RpcInvalidRequestException : RpcException
	{
		public RpcInvalidRequestException(string message) : base(RpcErrorCode.InvalidRequest, message)
		{
		}
	}
	public class RpcAmbiguousMethodException : RpcException
	{
		public RpcAmbiguousMethodException() : base(RpcErrorCode.AmbiguousMethod, "Request matches multiple method signatures")
		{
		}
	}
	public class RpcMethodNotFoundException : RpcException
	{
		public RpcMethodNotFoundException() : base(RpcErrorCode.MethodNotFound, "No method found with the requested signature")
		{
		}
	}
	public class RpcInvalidParametersException : RpcException
	{
		public RpcInvalidParametersException() : base(RpcErrorCode.InvalidParams, "Parameters do not match")
		{
		}
	}
	public class RpcUnknownException : RpcException
	{
		public RpcUnknownException(string message) : base(RpcErrorCode.InternalError, message)
		{
		}
	}


	//Not a request exception so it is not an `RpcException`
	public class RpcConfigurationException : Exception
	{
		public RpcConfigurationException(string message) : base(message)
		{
		}
	}
}
