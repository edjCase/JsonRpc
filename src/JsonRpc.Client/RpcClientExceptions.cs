using System;
using edjCase.JsonRpc.Core;

namespace edjCase.JsonRpc.Client
{
	/// <summary>
	/// Base exception that is thrown from an error that was caused by the client
	/// for the rpc request (not caused by rpc server)
	/// </summary>
	public abstract class RpcClientException : Exception
	{
		/// <param name="message">Error message</param>
		protected RpcClientException(string message) : base(message)
		{

		}

		/// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception</param>
		protected RpcClientException(string message, Exception innerException) : base(message, innerException)
		{

		}
	}

	/// <summary>
	/// Exception for all unknown exceptions that were thro
	/// </summary>
	public class RpcClientUnknownException : RpcClientException
	{
		/// <param name="message">Error message</param>
		public RpcClientUnknownException(string message) : base(message)
		{
		}

		/// <param name="message">Error message</param>
		/// <param name="innerException">Inner exception</param>
		public RpcClientUnknownException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
