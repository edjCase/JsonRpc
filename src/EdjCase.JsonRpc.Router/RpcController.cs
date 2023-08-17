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
		[NonRpcMethod]
		protected virtual RpcMethodSuccessResult Ok(object? obj = null)
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
		[NonRpcMethod]
		protected virtual RpcMethodErrorResult Error(int errorCode, string message, object? data = null)
		{
			return new RpcMethodErrorResult(errorCode, message, data);
		}
	}

	/// <summary>
	/// Attribute to decorate a derived <see cref="RpcController"/> class
	/// Allows setting a custom route name instead of using the controller name
	/// </summary>
	public class RpcRouteAttribute : Attribute
	{
		/// <summary>
		/// Name of the route to be used in the router. If unspecified, will use controller name.
		/// </summary>
		public string? RouteName { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="routeName">(Optional) Name of the route to be used in the router. If unspecified, will use controller name.</param>
		public RpcRouteAttribute(string? routeName)
		{
			this.RouteName = routeName;
		}
	}

	/// <summary>
	/// Attribute to decorate a method from a derived <see cref="RpcController"/> class
	/// Allows a method to not be included as a method
	/// </summary>
	public class NonRpcMethodAttribute : Attribute
	{

	}
}
