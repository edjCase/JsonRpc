using System.IO;
using EdjCase.JsonRpc.Common;

namespace EdjCase.JsonRpc.Router
{
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
		public RpcResponse(RpcId id, object? result) : this(id)
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
		public object? Result { get; private set; }

		/// <summary>
		/// Error from processing Rpc request (Required)
		/// </summary>
		public RpcError? Error { get; private set; }

		public bool HasError => this.Error != null;

		public void ThrowErrorIfExists()
		{
			if (this.HasError)
			{
				throw this.Error!.CreateException();
			}
		}
	}
}