using System;
using System.Text.Json;
using EdjCase.JsonRpc.Router.Abstractions;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Configuration data for the Rpc server that is shared between all middlewares
	/// </summary>
	public class RpcServerConfiguration
	{
		/// <summary>
		/// Json serialization settings that will be used in serialization and deserialization
		/// for rpc requests
		/// </summary>
		public JsonSerializerOptions? JsonSerializerSettings { get; set; }


		/// <summary>
		/// If true will show exception messages that the server rpc methods throw. Defaults to false
		/// </summary>
		public bool ShowServerExceptions { get; set; }

		/// <summary>
		/// If specified the router will throw an error if there is a batch request count
		/// greater than the limit
		/// </summary>
		public int? BatchRequestLimit { get; set; }

		public Func<ExceptionContext, OnExceptionResult>? OnInvokeException { get; set; }

		/// <summary>
		/// Max wait time for the server to shutdown to wait for fire and forget requests
		/// to complete
		/// </summary>
		public TimeSpan? ShutdownTimeoutOverride { get; set; }
	}

	public class ExceptionContext
	{
		public RpcRequest Request { get; }
		public IServiceProvider ServiceProvider { get; }
		public Exception Exception { get; }
		public ExceptionContext(RpcRequest request, IServiceProvider serviceProvider, Exception exception)
		{
			this.Request = request;
			this.ServiceProvider = serviceProvider;
			this.Exception = exception;
		}
	}

	public class OnExceptionResult
	{
		public bool ThrowException { get; }
		public object? ResponseObject { get; }

		private OnExceptionResult(bool throwException, object? responseObject)
		{
			this.ThrowException = throwException;
			this.ResponseObject = responseObject;
		}

		public static OnExceptionResult UseObjectResponse(object responseObject)
		{
			return new OnExceptionResult(false, responseObject);
		}

		public static OnExceptionResult UseMethodResultResponse(IRpcMethodResult result)
		{
			return new OnExceptionResult(false, result);
		}

		public static OnExceptionResult UseExceptionResponse(Exception ex)
		{
			return new OnExceptionResult(true, ex);
		}

		public static OnExceptionResult DontHandle()
		{
			return new OnExceptionResult(true, null);
		}
	}
}
