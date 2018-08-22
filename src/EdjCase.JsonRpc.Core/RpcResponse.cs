using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace EdjCase.JsonRpc.Core
{
	public class RpcResponse<T> : RpcResponse
	{
		public RpcResponse(RpcId id, T result)
			:base(id, result)
		{

		}

		public RpcResponse(RpcId id, RpcError error)
			:base(id, error)
		{
			
		}

		public new T Result => (T)base.Result;
		

		public static RpcResponse<T> FromResponse(RpcResponse response)
		{
			if (response.HasError)
			{
				return new RpcResponse<T>(response.Id, response.Error);
			}
			return new RpcResponse<T>(response.Id, (T)response.Result);
		}
	}

	public class RpcResponse
	{
		protected RpcResponse()
		{
		}

		/// <param name="id">Request id</param>
		protected RpcResponse(RpcId id)
		{
			this.Id = id;
		}

		/// <param name="id">Request id</param>
		/// <param name="error">Request error</param>
		public RpcResponse(RpcId id, RpcError error) : this(id)
		{
			this.Error = error;
		}

		/// <param name="id">Request id</param>
		/// <param name="result">Response result object</param>
		public RpcResponse(RpcId id, object result) : this(id)
		{
			this.Result = result;
		}

		/// <summary>
		/// Request id
		/// </summary>
		public RpcId Id { get; private set; }

		/// <summary>
		/// Reponse result object (Required)
		/// </summary>
		public object Result { get; private set; }

		/// <summary>
		/// Error from processing Rpc request (Required)
		/// </summary>
		public RpcError Error { get; private set; }

		public bool HasError => this.Error != null;

		public void ThrowErrorIfExists()
		{
			if (this.HasError)
			{
				throw this.Error.CreateException();
			}
		}
	}

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

		public new T Data => (T)base.Data;
	}

	/// <summary>
	/// Model to represent an Rpc response error
	/// </summary>
	public class RpcError
	{

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(RpcErrorCode code, string message, object data = null)
			: this((int)code, message, data)
		{
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
		public object Data { get; }

		/// <summary>
		/// Type of the data object
		/// </summary>
		public Type DataType { get; }

		public RpcException CreateException()
		{
			return new RpcException(this.Code, this.Message, data: this.Data);
		}

	}
}