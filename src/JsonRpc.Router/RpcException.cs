using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public abstract class RpcException : Exception
	{
		public int RpcErrorCode { get; }
		public object RpcData { get; }
		public RpcException(int errorCode, string message, object data = null) : base(message)
		{
			this.RpcErrorCode = errorCode;
			this.RpcData = data;
		}
	}
	public class InvalidRpcRequestException : RpcException
	{
		public InvalidRpcRequestException(string message) : base(-32600, message)
		{
		}
	}
	public class AmbiguousRpcMethodException : RpcException
	{
		public AmbiguousRpcMethodException() : base(-32000, "Request matches multiple method signatures")
		{
		}
	}
	public class RpcMethodNotFoundException : RpcException
	{
		public RpcMethodNotFoundException() : base(-32601, "No method found with the requested signature")
		{
		}
	}
	public class InvalidRpcParametersException : RpcException
	{
		public InvalidRpcParametersException() : base(-32602, "Parameters do not match")
		{
		}
	}
}
