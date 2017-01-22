using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EdjCase.JsonRpc.Router.Defaults
{
	/// <summary>
	/// Default Rpc method invoker that uses asynchronous processing
	/// </summary>
	public class DefaultRpcInvoker : IRpcInvoker
	{
		/// <summary>
		/// Logger for logging Rpc invocation
		/// </summary>
		private ILogger<DefaultRpcInvoker> logger { get; }

		/// <summary>
		/// AspNet service to authorize requests
		/// </summary>
		private IAuthorizationService authorizationService { get; }
		/// <summary>
		/// Provides authorization policies for the authroziation service
		/// </summary>
		private IAuthorizationPolicyProvider policyProvider { get; }

		/// <summary>
		/// Configuration data for the server
		/// </summary>
		private IOptions<RpcServerConfiguration> serverConfig { get; }


		/// <param name="authorizationService">Service that authorizes each method for use if configured</param>
		/// <param name="policyProvider">Provides authorization policies for the authroziation service</param>
		/// <param name="logger">Optional logger for logging Rpc invocation</param>
		/// <param name="serverConfig">Configuration data for the server</param>
		public DefaultRpcInvoker(IAuthorizationService authorizationService, IAuthorizationPolicyProvider policyProvider, ILogger<DefaultRpcInvoker> logger, IOptions<RpcServerConfiguration> serverConfig)
		{
			this.authorizationService = authorizationService;
			this.policyProvider = policyProvider;
			this.logger = logger;
			this.serverConfig = serverConfig;
		}


		/// <summary>
		/// Call the incoming Rpc requests methods and gives the appropriate respones
		/// </summary>
		/// <param name="requests">List of Rpc requests</param>
		/// <param name="route">Rpc route that applies to the current request</param>
		/// <param name="httpContext">The context of the current http request</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <returns>List of Rpc responses for the requests</returns>
		public async Task<List<RpcResponse>> InvokeBatchRequestAsync(List<RpcRequest> requests, RpcRoute route, IRouteContext routeContext, JsonSerializerSettings jsonSerializerSettings = null)
		{
			this.logger?.LogDebug($"Invoking '{requests.Count}' batch requests");
			var invokingTasks = new List<Task<RpcResponse>>();
			foreach (RpcRequest request in requests)
			{
				Task<RpcResponse> invokingTask = Task.Run(async () => await this.InvokeRequestAsync(request, route, routeContext, jsonSerializerSettings));
				if (request.Id != null)
				{
					//Only wait for non-notification requests
					invokingTasks.Add(invokingTask);
				}
			}

			await Task.WhenAll(invokingTasks.ToArray());

			List<RpcResponse> responses = invokingTasks
				.Select(t => t.Result)
				.Where(r => r != null)
				.ToList();

			this.logger?.LogDebug($"Finished '{requests.Count}' batch requests");

			return responses;
		}

		/// <summary>
		/// Call the incoming Rpc request method and gives the appropriate response
		/// </summary>
		/// <param name="request">Rpc request</param>
		/// <param name="route">Rpc route that applies to the current request</param>
		/// <param name="httpContext">The context of the current http request</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <returns>An Rpc response for the request</returns>
		public async Task<RpcResponse> InvokeRequestAsync(RpcRequest request, RpcRoute route, IRouteContext routeContext, JsonSerializerSettings jsonSerializerSettings = null)
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

			this.logger?.LogDebug($"Invoking request with id '{request.Id}'");
			RpcResponse rpcResponse;
			try
			{
				if (!string.Equals(request.JsonRpcVersion, JsonRpcContants.JsonRpcVersion))
				{
					throw new RpcInvalidRequestException($"Request must be jsonrpc version '{JsonRpcContants.JsonRpcVersion}'");
				}

				object[] parameterList;
				RpcMethod rpcMethod = this.GetMatchingMethod(route, request, out parameterList, routeContext.RequestServices, jsonSerializerSettings);

				bool isAuthorized = await this.IsAuthorizedAsync(rpcMethod, routeContext);

				if (isAuthorized)
				{

					this.logger?.LogDebug($"Attempting to invoke method '{request.Method}'");
					object result = await rpcMethod.InvokeAsync(parameterList);
					this.logger?.LogDebug($"Finished invoking method '{request.Method}'");

					JsonSerializer jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
					if (result is IRpcMethodResult)
					{
						this.logger?.LogTrace($"Result is {nameof(IRpcMethodResult)}.");
						rpcResponse = ((IRpcMethodResult)result).ToRpcResponse(request.Id, obj => JToken.FromObject(obj, jsonSerializer));
					}
					else
					{
						this.logger?.LogTrace($"Result is plain object.");
						JToken resultJToken = result != null ? JToken.FromObject(result, jsonSerializer) : null;
						rpcResponse = new RpcResponse(request.Id, resultJToken);
					}
				}
				else
				{
					var authError = new RpcError(RpcErrorCode.InvalidRequest, "Unauthorized");
					rpcResponse = new RpcResponse(request.Id, authError);
				}
			}
			catch (RpcException ex)
			{
				this.logger?.LogException(ex, "An Rpc error occurred. Returning an Rpc error response");
				RpcError error = new RpcError(ex, this.serverConfig.Value.ShowServerExceptions);
				rpcResponse = new RpcResponse(request.Id, error);
			}
			catch (Exception ex)
			{
				rpcResponse = this.GetUnknownExceptionReponse(request, ex);
			}

			if (request.Id != null)
			{
				this.logger?.LogDebug($"Finished request with id '{request.Id}'");
				//Only give a response if there is an id
				return rpcResponse;
			}
			this.logger?.LogDebug($"Finished request with no id. Not returning a response");
			return null;
		}

		private async Task<bool> IsAuthorizedAsync(RpcMethod rpcMethod, IRouteContext routeContext)
		{
			if (rpcMethod.AuthorizeDataList.Any())
			{
				if (rpcMethod.AllowAnonymous)
				{
					this.logger?.LogDebug("Skipping authorization. Allow anonymous specified for method.");
				}
				else
				{
					this.logger?.LogDebug($"Running authorization for method.");
					AuthorizationPolicy policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, rpcMethod.AuthorizeDataList);
					bool passed = await this.authorizationService.AuthorizeAsync(routeContext.User, policy);
					if (!passed)
					{
						this.logger?.LogInformation($"Authorization failed for user '{routeContext.User.Identity.Name}'.");
						return false;
					}
					else
					{
						this.logger?.LogDebug($"Authoization was successful for user '{routeContext.User.Identity.Name}'.");
					}
				}
			}
			else
			{
				this.logger?.LogDebug("Skipping authorization. None configured for method.");
			}
			return true;
		}

		/// <summary>
		/// Converts an unknown caught exception into a Rpc response
		/// </summary>
		/// <param name="request">Current Rpc request</param>
		/// <param name="ex">Unknown exception</param>
		/// <returns>Rpc error response from the exception</returns>
		private RpcResponse GetUnknownExceptionReponse(RpcRequest request, Exception ex)
		{
			this.logger?.LogException(ex, "An unknown error occurred. Returning an Rpc error response");

			RpcUnknownException exception = new RpcUnknownException("An internal server error has occurred", ex);
			RpcError error = new RpcError(exception, this.serverConfig.Value.ShowServerExceptions);
			if (request?.Id == null)
			{
				return null;
			}
			RpcResponse rpcResponse = new RpcResponse(request.Id, error);
			return rpcResponse;
		}

		/// <summary>
		/// Finds the matching Rpc method for the current request
		/// </summary>
		/// <param name="route">Rpc route for the current request</param>
		/// <param name="request">Current Rpc request</param>
		/// <param name="parameterList">Paramter list parsed from the request</param>
		/// <param name="serviceProvider">(Optional)IoC Container for rpc method controllers</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <returns>The matching Rpc method to the current request</returns>
		private RpcMethod GetMatchingMethod(RpcRoute route, RpcRequest request, out object[] parameterList, IServiceProvider serviceProvider = null, JsonSerializerSettings jsonSerializerSettings = null)
		{
			if (route == null)
			{
				throw new ArgumentNullException(nameof(route));
			}
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			this.logger?.LogDebug($"Attempting to match Rpc request to a method '{request.Method}'");
			List<RpcMethod> methods = DefaultRpcInvoker.GetRpcMethods(route, serviceProvider, jsonSerializerSettings);

			//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
			methods = methods
				.Where(m => string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase))
				.ToList();

			RpcMethod rpcMethod = null;
			parameterList = null;
			int originalMethodCount = methods.Count;
			if (methods.Count > 0)
			{
				List<RpcMethod> potentialMatches = new List<RpcMethod>();
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
						potentialMatches.Add(method);
					}
				}

				if (potentialMatches.Count > 1)
				{
					//Try to remove ambiguity with case sensitive check
					potentialMatches = potentialMatches
						.Where(m => string.Equals(m.Method, request.Method, StringComparison.Ordinal))
						.ToList();
					if (potentialMatches.Count != 1) 
					{
						this.logger?.LogError("More than one method matched the rpc request. Unable to invoke due to ambiguity.");
						throw new RpcMethodNotFoundException();
					}
				}

				if (potentialMatches.Count == 1)
				{
					rpcMethod = potentialMatches.First();
				}
			}
			if (rpcMethod == null)
			{
				this.logger?.LogError("No methods matched request.");
				throw new RpcMethodNotFoundException();
			}
			this.logger?.LogDebug("Request was matched to a method");
			return rpcMethod;
		}

		/// <summary>
		/// Gets all the predefined Rpc methods for a Rpc route
		/// </summary>
		/// <param name="route">The route to get Rpc methods for</param>
		/// <param name="serviceProvider">(Optional) IoC Container for rpc method controllers</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <returns>List of Rpc methods for the specified Rpc route</returns>
		private static List<RpcMethod> GetRpcMethods(RpcRoute route, IServiceProvider serviceProvider = null, JsonSerializerSettings jsonSerializerSettings = null)
		{
			List<RpcMethod> rpcMethods = new List<RpcMethod>();
			foreach (RouteCriteria routeCriteria in route.RouteCriteria)
			{
				foreach (Type type in routeCriteria.Types)
				{
					MethodInfo[] publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
					foreach (MethodInfo publicMethod in publicMethods)
					{
						RpcMethod rpcMethod = new RpcMethod(type, route, publicMethod, serviceProvider, jsonSerializerSettings);
						rpcMethods.Add(rpcMethod);
					}
				}
			}
			return rpcMethods;
		}

	}
}
