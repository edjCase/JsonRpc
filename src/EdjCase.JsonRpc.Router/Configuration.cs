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

		/// <summary>
		/// If specified the router will call the specified method if the invoker throws an exception.
		/// The returned result object will allow handling the error 
		/// </summary>
		public Func<ExceptionContext, OnExceptionResult>? OnInvokeException { get; set; }

		/// <summary>
		/// If specified the router will call the specified method when the invoker is called.
		/// </summary>
		public Action<RpcInvokeContext>? OnInvokeStart { get; set; }

		/// <summary>
		/// If specified the router will call the specified method when the invoker has finished.
		/// </summary>
		public Action<RpcInvokeContext, RpcResponse>? OnInvokeEnd { get; set; }
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



	public class RpcInvokeContext
	{

		/// <summary>
		/// Dependency injection service provider for the request.
		/// </summary>
		public IServiceProvider ServiceProvider { get; }

		/// <summary>
		/// Current request object.
		/// </summary>
		public RpcRequest Request { get; }

		/// <summary>
		/// Current request path
		/// </summary>
		public RpcPath? Path { get; }

		/// <summary>
		/// Settable contextual data for cross-event data, such as timers for start->end of a request
		/// </summary>
		public object? CustomContextData { get; set; }

		internal RpcInvokeContext(IServiceProvider serviceProvider, RpcRequest request, RpcPath? path)
		{
			this.ServiceProvider = serviceProvider;
			this.Request = request;
			this.Path = path;
		}
	}
}
