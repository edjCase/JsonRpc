using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using EdjCase.JsonRpc.Router;
using System.IO;
using System.Buffers;

namespace EdjCase.JsonRpc.Router.Defaults
{
	/// <summary>
	/// Default Rpc method invoker that uses asynchronous processing
	/// </summary>
	internal class DefaultRpcInvoker : IRpcInvoker
	{
		/// <summary>
		/// Logger for logging Rpc invocation
		/// </summary>
		private ILogger<DefaultRpcInvoker> logger { get; }

		/// <summary>
		/// Configuration data for the server
		/// </summary>
		private IOptions<RpcServerConfiguration> serverConfig { get; }

		/// <summary>
		/// Matches the route method name and parameters to the correct method to execute
		/// </summary>
		private IRpcRequestMatcher rpcRequestMatcher { get; }
		private IRpcContextAccessor contextAccessor { get; }
		private IRpcAuthorizationHandler authorizationHandler { get; }
		private IRpcFireAndForgetTaskPool fireAndForgetTaskPool { get; }
		private ILoggerFactory loggerFactory { get; }

		private static ConcurrentDictionary<Type, ObjectFactory> objectFactoryCache { get; } = new ConcurrentDictionary<Type, ObjectFactory>();

		/// <param name="authorizationService">Service that authorizes each method for use if configured</param>
		/// <param name="policyProvider">Provides authorization policies for the authroziation service</param>
		/// <param name="logger">Optional logger for logging Rpc invocation</param>
		/// <param name="serverConfig">Configuration data for the server</param>
		/// <param name="rpcRequestMatcher">Matches the route method name and parameters to the correct method to execute</param>
		public DefaultRpcInvoker(ILogger<DefaultRpcInvoker> logger,
			IOptions<RpcServerConfiguration> serverConfig,
			IRpcRequestMatcher rpcRequestMatcher,
			IRpcContextAccessor contextAccessor,
			IRpcAuthorizationHandler authorizationHandler,
			IRpcFireAndForgetTaskPool fireAndForgetTaskQueue,
			ILoggerFactory loggerFactory)
		{
			this.logger = logger;
			this.serverConfig = serverConfig;
			this.rpcRequestMatcher = rpcRequestMatcher;
			this.contextAccessor = contextAccessor;
			this.authorizationHandler = authorizationHandler;
			this.fireAndForgetTaskPool = fireAndForgetTaskQueue;
			this.loggerFactory = loggerFactory;
		}


		/// <summary>
		/// Call the incoming Rpc requests methods and gives the appropriate respones
		/// </summary>
		/// <param name="requests">List of Rpc requests</param>
		/// <param name="routeContext">The context of the current request</param>
		/// <param name="path">Rpc path that applies to the current request</param>
		/// <returns>List of Rpc responses for the requests</returns>
		public async Task<List<RpcResponse>> InvokeBatchRequestAsync(IList<RpcRequest> requests)
		{
			this.logger.InvokingBatchRequests(requests.Count);
			Task<RpcResponse?>[] invokingTasks = requests
				.Select(r => this.InvokeRequestAsync(r))
				.ToArray();

			await Task.WhenAll(invokingTasks);

			List<RpcResponse> responses = invokingTasks
				.Where(t => t.Result != null)
				.Select(t => t.Result!)
				.ToList();

			this.logger.BatchRequestsComplete();

			return responses;
		}

		/// <summary>
		/// Call the incoming Rpc request method and gives the appropriate response
		/// </summary>
		/// <param name="request">Rpc request</param>
		/// <param name="path">Rpc path that applies to the current request</param>
		/// <param name="routeContext">The context of the current rpc request</param>
		/// <returns>An Rpc response for the request</returns>
		public Task<RpcResponse?> InvokeRequestAsync(RpcRequest request)
		{
			if (!request.Id.HasValue)
			{
				this.InvokeInBackground(request);
				return Task.FromResult<RpcResponse?>(null);
			}
			//Only wait and give response for requests with ids
			return this.InvokeInForground(request);
		}

		private void InvokeInBackground(RpcRequest request)
		{
			//Create new logger because logger may be disposed if using request logging
			ILogger logger = this.loggerFactory.CreateLogger("Background Request Invoke");
			async Task NoReponseInvoke()
			{
				try
				{
					(bool isError, object? result) = await this.InvokeRequestInternalAsync(request);
					if (isError)
					{
						var error = (RpcError)result!;
						throw error.CreateException();
					}
					logger.FinishedRequestNoId();
				}
				catch (Exception ex)
				{
					logger.LogException(ex, "Failed processing request in the background");
				}
			}
			//Execute non-id requests in the 'background'. No response is given for the request
			this.fireAndForgetTaskPool.Add(NoReponseInvoke);
		}

		private async Task<RpcResponse?> InvokeInForground(RpcRequest request)
		{
			bool isError;
			object? result;
			try
			{
				(isError, result) = await this.InvokeRequestInternalAsync(request); ;
			}
			catch (Exception ex)
			{
				const string errorMessage = "An Rpc error occurred while trying to invoke request.";
				this.logger.LogException(ex, errorMessage);
				RpcError error;
				if (ex is RpcException rpcException)
				{
					error = rpcException.ToRpcError(this.serverConfig.Value.ShowServerExceptions);
				}
				else
				{
					error = new RpcError(RpcErrorCode.InternalError, errorMessage, ex);
				}
				isError = true;
				result = error;
			}
			if (isError)
			{
				var error = (RpcError)result!;
				return new RpcResponse(request.Id, error);
			}
			else
			{
				RpcResponse response;
				if (result is IRpcMethodResult methodResult)
				{
					response = methodResult.ToRpcResponse(request.Id);
				}
				else
				{
					response = new RpcResponse(request.Id, result);
				}

				this.logger.FinishedRequest(request.Id);
				return response;
			}
		}

		private async Task<(bool, object?)> InvokeRequestInternalAsync(RpcRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			this.logger.InvokingRequest(request.Id);
			RpcMethodInfo rpcMethod;
			using (var requestSignature = RpcRequestSignature.Create(request))
			{
				rpcMethod = this.rpcRequestMatcher.GetMatchingMethod(requestSignature);
			}

			bool isAuthorized = await this.authorizationHandler.IsAuthorizedAsync(rpcMethod.MethodInfo);

			if (isAuthorized)
			{
				object[] realParameters = this.ParseParameters(request.Parameters, rpcMethod.Parameters);

				this.logger.InvokeMethod(request.Method);
				IRpcContext routeContext = this.contextAccessor.Value!;
				object? result = await this.InvokeAsync(rpcMethod.MethodInfo, realParameters, request, routeContext.RequestServices);
				this.logger.InvokeMethodComplete(request.Method);
				return (false, result);
			}
			else
			{
				var authError = new RpcError(RpcErrorCode.InvalidRequest, "Unauthorized");
				return (true, authError);
			}
		}

		private object[] ParseParameters(RpcParameters? requestParameters, RpcParameterInfo[] methodParameters)
		{
			object[] paramCache = ArrayPool<object>.Shared.Rent(methodParameters.Length);
			try
			{
				if (requestParameters != null && requestParameters.Any())
				{
					IRpcParameter[] parameterList;
					if (requestParameters.IsDictionary)
					{
						if (!this.TryParseParameterList(methodParameters, requestParameters.AsDictionary, out IRpcParameter[]? pList))
						{
							string message = "Unable to parse the ";
							throw new RpcException(RpcErrorCode.InternalError, message);
						}
						parameterList = pList!;
					}
					else
					{
						parameterList = requestParameters.AsArray;
					}
					List<RpcParameterInfo>? badParams = null;
					for (int i = 0; i < parameterList!.Length; i++)
					{
						RpcParameterInfo parameterInfo = methodParameters[i];
						IRpcParameter parameter = parameterList[i];
						if (!parameter.TryGetValue(parameterInfo.RawType, out object? value))
						{
							if (badParams == null)
							{
								int parametersRemaining = parameterList.Length - i;
								badParams = new List<RpcParameterInfo>(parametersRemaining);
							}
							badParams.Add(parameterInfo);
							continue;
						}
						paramCache[i] = value!;
					}
					if (badParams != null)
					{
						string message = string.Join("\n", badParams.Select(p => $"Unable to parse parameter '{p.Name}' to type '{p.RawType}'"));
						throw new RpcException(RpcErrorCode.InvalidParams, message);
					}
					//Only make an array if needed
					var deserializedParameters = new object[methodParameters.Length];
					paramCache
						.AsSpan(0, methodParameters.Length)
						.CopyTo(deserializedParameters);
					return deserializedParameters;
				}
				else
				{
					return new object[methodParameters.Length];
				}
			}
			finally
			{
				ArrayPool<object>.Shared.Return(paramCache, clearArray: false);
			}
		}

		private bool TryParseParameterList(RpcParameterInfo[] methodParameters, Dictionary<string, IRpcParameter> requestParameters, out IRpcParameter[]? parameterList)
		{
			if (methodParameters.Length > requestParameters.Count)
			{
				parameterList = null;
				return false;
			}
			if (methodParameters.Length > requestParameters.Count)
			{
				//The method param count can be larger as long as the diff is optional parameters
				if (methodParameters.Count(p => !p.IsOptional) > requestParameters.Count)
				{
					parameterList = null;
					return false;
				}
			}
			parameterList = new IRpcParameter[requestParameters.Count];
			for (int i = 0; i < requestParameters.Count; i++)
			{
				RpcParameterInfo parameterInfo = methodParameters[i];

				foreach (KeyValuePair<string, IRpcParameter> requestParameter in requestParameters)
				{
					if (RpcUtil.NamesMatch(parameterInfo.Name.AsSpan(), requestParameter.Key.AsSpan()))
					{
						//TODO do we care about the case where 2+ parameters have very similar names and types?
						parameterList[i] = requestParameter.Value;
						break;
					}
				}
				if (parameterList[i] == null)
				{
					//Doesn't match the names of any
					return false;
				}
			}
			return true;
		}




		/// <summary>
		/// Invokes the method with the specified parameters, returns the result of the method
		/// </summary>
		/// <exception cref="RpcInvalidParametersException">Thrown when conversion of parameters fails or when invoking the method is not compatible with the parameters</exception>
		/// <param name="parameters">List of parameters to invoke the method with</param>
		/// <returns>The result of the invoked method</returns>
		private async Task<object?> InvokeAsync(MethodInfo methodInfo, object[] parameters, RpcRequest request, IServiceProvider serviceProvider)
		{
			//Use service provider to create instance
			ObjectFactory objectFactory = DefaultRpcInvoker.objectFactoryCache.GetOrAdd(methodInfo.DeclaringType, (t) => ActivatorUtilities.CreateFactory(t, Array.Empty<Type>()));
			object obj = objectFactory(serviceProvider, null);
			if (obj == null)
			{
				//Use reflection to create instance if service provider failed or is null
				obj = Activator.CreateInstance(methodInfo.DeclaringType);
			}
			try
			{
				object? returnObj = methodInfo.Invoke(obj, parameters);

				returnObj = await DefaultRpcInvoker.HandleAsyncResponses(returnObj);

				return returnObj;
			}
			catch (TargetInvocationException ex)
			{
				//Controller error handling
				if (this.serverConfig.Value.OnInvokeException != null)
				{
					var context = new ExceptionContext(request, serviceProvider, ex.InnerException);
					OnExceptionResult result = this.serverConfig.Value.OnInvokeException(context);
					if (!result.ThrowException)
					{
						return result.ResponseObject!;
					}
					if (result.ResponseObject is Exception rEx)
					{
						throw rEx;
					}
				}
				throw new RpcException(RpcErrorCode.InternalError, "Exception occurred from target method execution.", ex);
			}
			catch (Exception ex)
			{
				throw new RpcException(RpcErrorCode.InvalidParams, "Exception from attempting to invoke method. Possibly invalid parameters for method.", ex);
			}
		}

		/// <summary>
		/// Handles/Awaits the result object if it is a async Task
		/// </summary>
		/// <param name="returnObj">The result of a invoked method</param>
		/// <returns>Awaits a Task and returns its result if object is a Task, otherwise returns the same object given</returns>
		private static async Task<object?> HandleAsyncResponses(object? returnObj)
		{
			Task? task = returnObj as Task;
			if (task == null) //Not async request
			{
				return returnObj;
			}
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				throw new TargetInvocationException(ex);
			}
			PropertyInfo propertyInfo = task.GetType().GetProperty("Result");
			if (propertyInfo != null)
			{
				//Type of Task<T>. Wait for result then return it
				return propertyInfo.GetValue(returnObj);
			}
			//Just of type Task with no return result			
			return null;
		}
	}
}
