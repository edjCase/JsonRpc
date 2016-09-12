using System;
using System.Collections.Generic;
using EdjCase.JsonRpc.Core.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace EdjCase.JsonRpc.Core
{
	[JsonObject]
	public class RpcResponse
	{
		[JsonConstructor]
		protected RpcResponse()
		{
		}

		/// <param name="id">Request id</param>
		protected RpcResponse(object id)
		{
			this.Id = id;
		}

		/// <param name="id">Request id</param>
		/// <param name="error">Request error</param>
		public RpcResponse(object id, RpcError error) : this(id)
		{
			this.Error = error;
		}

		/// <param name="id">Request id</param>
		/// <param name="result">Response result object</param>
		public RpcResponse(object id, JToken result) : this(id)
		{
			this.Result = result;
		}

		/// <summary>
		/// Request id (Required but nullable)
		/// </summary>
		[JsonProperty("id", Required = Required.AllowNull)]
		[JsonConverter(typeof(RpcIdJsonConverter))]
		public object Id { get; private set; }

		/// <summary>
		/// Rpc request version (Required)
		/// </summary>
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpcVersion { get; private set; } = JsonRpcContants.JsonRpcVersion;

		/// <summary>
		/// Reponse result object (Required)
		/// </summary>
		[JsonProperty("result", Required = Required.Default)] //TODO somehow enforce this or an error, not both
		public JToken Result { get; private set; }

		/// <summary>
		/// Error from processing Rpc request (Required)
		/// </summary>
		[JsonProperty("error", Required = Required.Default)]
		public RpcError Error { get; private set; }

		[JsonIgnore]
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
	[JsonObject]
	public class RpcError
	{
		[JsonConstructor]
		private RpcError()
		{
		}

		/// <param name="exception">Exception from Rpc request</param>
		/// <param name="showServerExceptions">
		/// Optional. If true the inner exceptions to errors (possibly from server code) will be shown. Defaults to false.
		/// </param>
		public RpcError(RpcException exception, bool showServerExceptions)
		{
			if (exception == null)
			{
				throw new ArgumentNullException(nameof(exception));
			}
			this.Code = (int)exception.ErrorCode;
			this.Message = RpcError.GetErrorMessage(exception, showServerExceptions);
			this.Data = exception.RpcData;
		}

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(RpcErrorCode code, string message, JToken data = null) : this((int)code, message, data)
		{
		}

		/// <param name="code">Rpc error code</param>
		/// <param name="message">Error message</param>
		/// <param name="data">Optional error data</param>
		public RpcError(int code, string message, JToken data = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}
			this.Code = code;
			this.Message = message;
			this.Data = data;
		}

		/// <summary>
		/// Rpc error code (Required)
		/// </summary>
		[JsonProperty("code", Required = Required.Always)]
		public int Code { get; private set; }

		/// <summary>
		/// Error message (Required)
		/// </summary>
		[JsonProperty("message", Required = Required.Always)]
		public string Message { get; private set; }

		/// <summary>
		/// Error data (Optional)
		/// </summary>
		[JsonProperty("data")]
		public JToken Data { get; private set; }

		public RpcException CreateException()
		{
			RpcException exception;
			switch ((RpcErrorCode)this.Code)
			{
				case RpcErrorCode.ParseError:
					exception = new RpcParseException(this);
					break;
				case RpcErrorCode.InvalidRequest:
					exception = new RpcInvalidRequestException(this);
					break;
				case RpcErrorCode.MethodNotFound:
					exception = new RpcMethodNotFoundException(this);
					break;
				case RpcErrorCode.InvalidParams:
					exception = new RpcInvalidParametersException(this);
					break;
				case RpcErrorCode.InternalError:
					exception = new RpcInvalidParametersException(this);
					break;
				default:
					exception = new RpcCustomException(this);
					break;
			}
			return exception;
		}

		private static string GetErrorMessage(Exception exception, bool showServerExceptions)
		{
			string message = exception.Message;
			if (showServerExceptions && exception.InnerException != null)
			{
				message += "\tInner Exception: " + RpcError.GetErrorMessage(exception.InnerException, showServerExceptions);
			}
			return message;
		}
	}
}