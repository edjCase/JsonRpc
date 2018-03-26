using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace EdjCase.JsonRpc.Core
{
	public class RpcResponse
	{
		protected RpcResponse()
		{
		}

		/// <param name="id">Request id</param>
		protected RpcResponse(RpcId id)
		{
			if(id == default)
			{
				throw new ArgumentNullException(nameof(id));
			}
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

	/// <summary>
	/// Model to represent an Rpc response error
	/// </summary>
	public class RpcError
	{

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(RpcErrorCode code, string message, Exception serverException = null, object data = null) : this((int)code, message, serverException, data)
		{
		}

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(int code, string message, Exception serverException = null, object data = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}
			this.Code = code;
			this.message = message;
			this.Data = data;
			this.exception = serverException;
		}

		/// <summary>
		/// Rpc error code (Required)
		/// </summary>
		public int Code { get; }

		private string message { get; }

		private Exception exception { get; }

		/// <summary>
		/// Error message (Required)
		/// </summary>
		public string GetMessage(bool showServerExceptions = false)
		{
			if (showServerExceptions && this.exception != null)
			{
				if(this.message == null)
				{
					return this.exception.ToString();
				}
				return $"{this.message}{Environment.NewLine}Exception: {this.exception}";
			}
			return this.message;

		}

		/// <summary>
		/// Error data (Optional)
		/// </summary>
		public object Data { get; }

		public RpcException CreateException()
		{
			return new RpcException(this.Code, this.message, this.exception, this.Data);
		}
	}
}