using System;

namespace EdjCase.JsonRpc.Common
{
	public class RpcError<T> : RpcError
	{
		public RpcError(RpcErrorCode code, string message, T data)
			: base(code, message, data)
		{
		}

		public RpcError(int code, string message, T data)
			: base(code, message, data)
		{
		}

		public new T Data => (T)base.Data!;
	}

	/// <summary>
	/// Model to represent an Rpc response error
	/// </summary>
	public class RpcError
	{

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(RpcErrorCode code, string message, object? data = null)
			: this((int)code, message, data)
		{
		}

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(int code, string message, object? data = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}
			this.Code = code;
			this.Message = message;
			this.Data = data;
			this.DataType = data?.GetType();
		}

		/// <summary>
		/// Rpc error code (Required)
		/// </summary>
		public int Code { get; }

		public string Message { get; }

		/// <summary>
		/// Error data (Optional)
		/// </summary>
		public object? Data { get; }

		/// <summary>
		/// Type of the data object
		/// </summary>
		public Type? DataType { get; }

		public RpcException CreateException()
		{
			return new RpcException(this.Code, this.Message, data: this.Data);
		}

	}
}