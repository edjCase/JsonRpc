using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	[JsonObject]
	internal class RpcErrorResponse : RpcResponseBase
	{
		[JsonConstructor]
		private RpcErrorResponse()
		{

		}

		public RpcErrorResponse(string id, RpcError error) : base(id)
		{
			this.Error = error;
		}

		[JsonProperty("error")]
		public RpcError Error { get; private set; }
	}

	[JsonObject]
	internal class RpcResultResponse : RpcResponseBase
	{
		[JsonConstructor]
		private RpcResultResponse()
		{

		}

		public RpcResultResponse(string id, object result) : base(id)
		{
			this.Result = result;
		}
		[JsonProperty("result")]
		public object Result { get; private set; }
	}

	[JsonObject]
	internal abstract class RpcResponseBase
	{
		[JsonConstructor]
		protected RpcResponseBase()
		{

		}

		protected RpcResponseBase(string id)
		{
			this.Id = id;
		}

		[JsonProperty("id", Required = Required.AllowNull)]
		public string Id { get; private set; }
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpc { get; private set; } = "2.0";
	}

	[JsonObject]
	internal class RpcError
	{
		[JsonConstructor]
		private RpcError()
		{

		}

		public RpcError(RpcException exception)
		{
			if(exception == null)
			{
				throw new ArgumentNullException(nameof(exception));
			}
			this.Code = exception.RpcErrorCode;
			this.Message = exception.Message;
			this.Data = exception.RpcData;
		}

		public RpcError(int code, string message, object data = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}
			this.Code = code;
			this.Message = message;
			this.Data = data;
		}

		[JsonProperty("code", Required = Required.Always)]
		public int Code { get; private set; }
		[JsonProperty("message", Required = Required.Always)]
		public string Message { get; private set; }
		[JsonProperty("data")]
		public object Data { get; private set; }
	}
}
