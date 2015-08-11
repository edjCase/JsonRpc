using JsonRpc.Router.Abstractions;
using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JsonRpc.Router.Defaults
{
	public class DefaultRpcInvoker : IRpcInvoker
	{
		public ILogger Logger { get; set; }
		public DefaultRpcInvoker(ILogger logger = null)
		{
			this.Logger = logger;
		}

		public List<RpcResponseBase> InvokeBatchRequest(List<RpcRequest> requests, RpcRoute route)
		{
			this.Logger?.LogVerbose($"Invoking '{requests.Count}' batch requests");
			var invokingTasks = new List<Task<RpcResponseBase>>();
			foreach (RpcRequest request in requests)
			{
				Task<RpcResponseBase> invokingTask = Task.Run(() => this.InvokeRequest(request, route));
				invokingTasks.Add(invokingTask);
			}

			Task.WaitAll(invokingTasks.Cast<Task>().ToArray());

			List<RpcResponseBase> responses = invokingTasks
				.Select(t => t.Result)
				.Where(r => r != null)
				.ToList();

			this.Logger?.LogVerbose($"Finished '{requests.Count}' batch requests");

			return responses;
		}

		public RpcResponseBase InvokeRequest(RpcRequest request, RpcRoute route)
		{
			try
			{
				if (request == null)
				{
					throw new ArgumentNullException(nameof(request));
				}
				if (route == null)
				{
					throw new ArgumentNullException(nameof(route));
				}
			}
			catch (ArgumentNullException ex) // Dont want to throw any exceptions when doing async requests
			{
				return this.GetUnknownExceptionReponse(request, ex);
			}

			this.Logger?.LogVerbose($"Invoking request with id '{request.Id}'");
			RpcResponseBase rpcResponse;
			try
			{
				if (!string.Equals(request.JsonRpcVersion, "2.0"))
				{
					throw new RpcInvalidRequestException("Request must be jsonrpc version '2.0'");
				}
				
				object[] parameterList;
				RpcMethod rpcMethod = this.GetMatchingMethod(route, request, out parameterList);

				this.Logger?.LogVerbose($"Attempting to invoke method '{request.Method}'");
				object result = rpcMethod.Invoke(parameterList);
				this.Logger?.LogVerbose($"Finished invoking method '{request.Method}'");

				rpcResponse = new RpcResultResponse(request.Id, result);
			}
			catch (RpcException ex)
			{
				this.Logger?.LogError("An Rpc error occurred. Returning an Rpc error response", ex);
				RpcError error = new RpcError(ex);
				rpcResponse = new RpcErrorResponse(request.Id, error);
			}
			catch (Exception ex)
			{
				rpcResponse = this.GetUnknownExceptionReponse(request, ex);
			}

			if (request.Id != null)
			{
				this.Logger?.LogVerbose($"Finished request with id '{request.Id}'");
				//Only give a response if there is an id
				return rpcResponse;
			}
			this.Logger?.LogVerbose($"Finished request with no id. Not returning a response");
			return null;
		}

		private RpcResponseBase GetUnknownExceptionReponse(RpcRequest request, Exception ex)
		{
			this.Logger?.LogError("An unknown error occurred. Returning an Rpc error response", ex);
#if DEBUG
			string message = ex.Message;
#else
			string message = "An internal server error has occurred";
#endif
			RpcUnknownException exception = new RpcUnknownException(message);
			RpcError error = new RpcError(exception);
			if (request?.Id == null)
			{
				return null;
			}
			RpcResponseBase rpcResponse = new RpcErrorResponse(request.Id, error);
			return rpcResponse;
		}

		private RpcMethod GetMatchingMethod(RpcRoute route, RpcRequest request, out object[] parameterList)
		{
			if (route == null)
			{
				throw new ArgumentNullException(nameof(route));
			}
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			this.Logger?.LogVerbose($"Attempting to match Rpc request to a method '{request.Method}'");
			List<RpcMethod> methods = DefaultRpcInvoker.GetRpcMethods(route);

			methods = methods
				.Where(m => string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase))
				.ToList();

			RpcMethod rpcMethod = null;
			parameterList = null;
			if (methods.Count > 1)
			{
				foreach (RpcMethod method in methods)
				{
					bool matchingMethod;
					if (request.ParameterMap != null)
					{
						matchingMethod = method.HasParameterSignature(request.ParameterMap, out parameterList);
					}
					else
					{
						matchingMethod = method.HasParameterSignature(request.ParameterList);
						parameterList = request.ParameterList;
					}
					if (matchingMethod)
					{
						if (rpcMethod != null) //If already found a match
						{
							throw new RpcAmbiguousMethodException();
						}
						rpcMethod = method;
					}
				}
			}
			else if (methods.Count == 1)
			{
				//Only signature check for methods that have the same name for performance reasons
				rpcMethod = methods.First();
				if (request.ParameterMap != null)
				{
					bool signatureMatch = rpcMethod.TryParseParameterList(request.ParameterMap, out parameterList);
					if (!signatureMatch)
					{
						throw new RpcMethodNotFoundException();
					}
				}
				else
				{
					parameterList = request.ParameterList;
				}
			}
			if (rpcMethod == null)
			{
				throw new RpcMethodNotFoundException();
			}
			this.Logger?.LogVerbose("Request was matched to a method");
			return rpcMethod;
		}

		private static List<RpcMethod> GetRpcMethods(RpcRoute route)
		{
			List<RpcMethod> rpcMethods = new List<RpcMethod>();
			foreach (Type type in route.GetClasses())
			{
				MethodInfo[] publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				foreach (MethodInfo publicMethod in publicMethods)
				{
					RpcMethod rpcMethod = new RpcMethod(type, route, publicMethod);
					rpcMethods.Add(rpcMethod);
				}
			}
			return rpcMethods;
		}

	}
}
