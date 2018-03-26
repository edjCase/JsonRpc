using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public abstract class RpcController
	{
		/// <summary>
		/// Helper for returning a successful rpc response
		/// </summary>
		/// <param name="obj">Object to return in response</param>
		/// <returns>Success result for rpc response</returns>
		public virtual RpcMethodSuccessResult Ok(object obj = null)
		{
			return new RpcMethodSuccessResult(obj);
		}


		/// <summary>
		/// Helper for returning an error rpc response
		/// </summary>
		/// <param name="errorCode">JSON-RPC custom error code</param>
		/// <param name="message">(Optional)Error message</param>
		/// <param name="data">(Optional)Error data</param>
		/// <returns></returns>
		public virtual RpcMethodErrorResult Error(int errorCode, string message = null, Exception ex = null, object data = null)
		{
			return new RpcMethodErrorResult(errorCode, message, ex, data);
		}
	}
	
	public abstract class RpcErrorFilterAttribute : Attribute
	{
		public abstract OnExceptionResult OnException(RpcRouteInfo routeInfo, Exception ex);
	}

	public class OnExceptionResult
	{
		public bool ThrowException { get; }
		public object ResponseObject { get; }

		private OnExceptionResult(bool throwException, object responseObject)
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

#if !NETSTANDARD1_3
	/// <summary>
	/// Attribute to decorate a derived <see cref="RpcController"/> class
	/// </summary>
	public class RpcRouteAttribute : Attribute
	{
		/// <summary>
		/// Name of the route to be used in the router. If unspecified, will use controller name.
		/// </summary>
		public string RouteName { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="routeName">(Optional) Name of the route to be used in the router. If unspecified, will use controller name.</param>
		/// <param name="routeGroup">(Optional) Name of the group the route is in to allow route filtering per request.</param>
		public RpcRouteAttribute(string routeName = null)
		{
			this.RouteName = routeName?.Trim();
		}
	}
#endif
}
