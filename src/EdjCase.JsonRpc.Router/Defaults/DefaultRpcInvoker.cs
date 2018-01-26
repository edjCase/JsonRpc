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
using Microsoft.Extensions.DependencyInjection;
using EdjCase.JsonRpc.Router.Criteria;

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

		private JsonSerializer jsonSerializerCache { get; set; }

		private JsonSerializer GetJsonSerializer()
		{
			if (this.jsonSerializerCache == null)
			{
				this.jsonSerializerCache = this.serverConfig.Value?.JsonSerializerSettings == null
					? JsonSerializer.CreateDefault()
					: JsonSerializer.Create(this.serverConfig.Value.JsonSerializerSettings);
			}
			return this.jsonSerializerCache;
		}


		/// <param name="authorizationService">Service that authorizes each method for use if configured</param>
		/// <param name="policyProvider">Provides authorization policies for the authroziation service</param>
		/// <param name="logger">Optional logger for logging Rpc invocation</param>
		/// <param name="serverConfig">Configuration data for the server</param>
		public DefaultRpcInvoker(IAuthorizationService authorizationService, IAuthorizationPolicyProvider policyProvider,
			ILogger<DefaultRpcInvoker> logger, IOptions<RpcServerConfiguration> serverConfig)
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
		/// <param name="path">Rpc path that applies to the current request</param>
		/// <param name="httpContext">The context of the current http request</param>
		/// <returns>List of Rpc responses for the requests</returns>
		public async Task<List<RpcResponse>> InvokeBatchRequestAsync(List<RpcRequest> requests, RpcPath path, IRouteContext routeContext)
		{
			this.logger?.LogDebug($"Invoking '{requests.Count}' batch requests");
			var invokingTasks = new List<Task<RpcResponse>>();
			foreach (RpcRequest request in requests)
			{
				Task<RpcResponse> invokingTask = Task.Run(async () => await this.InvokeRequestAsync(request, path, routeContext));
				if (request.Id.HasValue)
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
		/// <param name="path">Rpc path that applies to the current request</param>
		/// <param name="httpContext">The context of the current http request</param>
		/// <returns>An Rpc response for the request</returns>
		public async Task<RpcResponse> InvokeRequestAsync(RpcRequest request, RpcPath path, IRouteContext routeContext)
		{
			try
			{
				if (request == null)
				{
					throw new ArgumentNullException(nameof(request));
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

				RpcMethodInfo rpcMethod = this.GetMatchingMethod(path, request, routeContext.RouteProvider, routeContext.RequestServices);

				bool isAuthorized = await this.IsAuthorizedAsync(rpcMethod.Method, routeContext);

				if (isAuthorized)
				{

					this.logger?.LogDebug($"Attempting to invoke method '{request.Method}'");
					object result = await this.InvokeAsync(rpcMethod, path, routeContext.RequestServices);
					this.logger?.LogDebug($"Finished invoking method '{request.Method}'");

					JsonSerializer jsonSerializer = this.GetJsonSerializer();
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

			if (request.Id.HasValue)
			{
				this.logger?.LogDebug($"Finished request with id: {request.Id}");
				//Only give a response if there is an id
				return rpcResponse;
			}
			this.logger?.LogDebug($"Finished request with no id. Not returning a response");
			return null;
		}

		private async Task<bool> IsAuthorizedAsync(MethodInfo methodInfo, IRouteContext routeContext)
		{
			IEnumerable<Attribute> customClassAttributes = methodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes();
			List<IAuthorizeData> authorizeDataListClass = customClassAttributes.OfType<IAuthorizeData>().ToList();
			IEnumerable<Attribute> customMethodAttributes = methodInfo.GetCustomAttributes();
			List<IAuthorizeData> authorizeDataListMethod = customMethodAttributes.OfType<IAuthorizeData>().ToList();

			if (authorizeDataListClass.Any() || authorizeDataListMethod.Any())
			{
				bool allowAnonymousOnClass = customClassAttributes.OfType<IAllowAnonymous>().Any();
				bool allowAnonymousOnMethod = customMethodAttributes.OfType<IAllowAnonymous>().Any();
				if (allowAnonymousOnClass || allowAnonymousOnMethod)
				{
					this.logger?.LogDebug("Skipping authorization. Allow anonymous specified for method.");
				}
				else
				{
					this.logger?.LogDebug($"Running authorization for method.");
					AuthorizationResult authResult = await this.CheckAuthorize(authorizeDataListClass, routeContext);
					if (authResult.Succeeded)
					{
						//Have to pass both controller and method authorize
						authResult = await this.CheckAuthorize(authorizeDataListMethod, routeContext);
					}
					if (authResult.Succeeded)
					{
						this.logger?.LogDebug($"Authorization was successful for user '{routeContext.User.Identity.Name}'.");
					}
					else
					{
						this.logger?.LogInformation($"Authorization failed for user '{routeContext.User.Identity.Name}'.");
						return false;
					}
				}
			}
			else
			{
				this.logger?.LogDebug("Skipping authorization. None configured for class or method.");
			}
			return true;
		}

		private async Task<AuthorizationResult> CheckAuthorize(List<IAuthorizeData> authorizeDataList, IRouteContext routeContext)
		{
			if (!authorizeDataList.Any())
			{
				return AuthorizationResult.Success();
			}
			AuthorizationPolicy policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeDataList);
			return await this.authorizationService.AuthorizeAsync(routeContext.User, policy);
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
		/// <param name="path">Rpc route for the current request</param>
		/// <param name="request">Current Rpc request</param>
		/// <param name="parameterList">Parameter list parsed from the request</param>
		/// <param name="serviceProvider">(Optional)IoC Container for rpc method controllers</param>
		/// <returns>The matching Rpc method to the current request</returns>
		private RpcMethodInfo GetMatchingMethod(RpcPath path, RpcRequest request, IRpcRouteProvider routeProvider, IServiceProvider serviceProvider)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			this.logger?.LogDebug($"Attempting to match Rpc request to a method '{request.Method}'");
			List<MethodInfo> allMethods = this.GetRpcMethods(path, routeProvider);

			//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
			List<MethodInfo> methodsWithSameName = allMethods
				.Where(m => string.Equals(m.Name, request.Method, StringComparison.OrdinalIgnoreCase))
				.ToList();

			var potentialMatches = new List<RpcMethodInfo>();
			foreach (MethodInfo method in methodsWithSameName)
			{
				(bool isMatch, RpcMethodInfo methodInfo) = this.HasParameterSignature(method, request);
				if (isMatch)
				{
					potentialMatches.Add(methodInfo);
				}
			}

			if (potentialMatches.Count > 1)
			{
				//Try to remove ambiguity with 'perfect matching' (usually optional params and types)
				List<RpcMethodInfo> exactMatches = potentialMatches
					.Where(p => p.HasExactParameterMatch())
					.ToList();
				if (exactMatches.Any())
				{
					potentialMatches = exactMatches;
				}
				if (potentialMatches.Count > 1)
				{
					//Try to remove ambiguity with case sensitive check
					potentialMatches = potentialMatches
						.Where(m => string.Equals(m.Method.Name, request.Method, StringComparison.Ordinal))
						.ToList();
					if (potentialMatches.Count != 1)
					{
						this.logger?.LogError("More than one method matched the rpc request. Unable to invoke due to ambiguity.");
						throw new RpcMethodNotFoundException();
					}
				}
			}

			RpcMethodInfo rpcMethod = null;
			if (potentialMatches.Count == 1)
			{
				rpcMethod = potentialMatches.First();
			}

			if (rpcMethod == null)
			{
				//Log diagnostics 
				string methodsString = string.Join(", ", allMethods.Select(m => m.Name));
				this.logger?.LogTrace("Methods in route: " + methodsString);

				var methodInfoList = new List<string>();
				foreach (MethodInfo matchedMethod in methodsWithSameName)
				{
					var parameterTypeList = new List<string>();
					foreach (ParameterInfo parameterInfo in matchedMethod.GetParameters())
					{
						string parameterType = parameterInfo.Name + ": " + parameterInfo.ParameterType.Name;
						if (parameterInfo.IsOptional)
						{
							parameterType += "(Optional)";
						}
						parameterTypeList.Add(parameterType);
					}
					string parameterString = string.Join(", ", parameterTypeList);
					methodInfoList.Add($"{{Name: '{matchedMethod.Name}', Parameters: [{parameterString}]}}");
				}
				this.logger?.LogTrace("Methods that matched the same name: " + string.Join(", ", methodInfoList));
				this.logger?.LogError("No methods matched request.");
				throw new RpcMethodNotFoundException();
			}
			this.logger?.LogDebug("Request was matched to a method");
			return rpcMethod;
		}

		/// <summary>
		/// Gets all the predefined Rpc methods for a Rpc route
		/// </summary>
		/// <param name="path">The route to get Rpc methods for</param>
		/// <param name="serviceProvider">(Optional) IoC Container for rpc method controllers</param>
		/// <returns>List of Rpc methods for the specified Rpc route</returns>
		private List<MethodInfo> GetRpcMethods(RpcPath path, IRpcRouteProvider routeProvider)
		{
			var methods = new List<MethodInfo>();
			foreach (IRpcMethodProvider methodProvider in routeProvider.GetMethodsByPath(path))
			{
				foreach (MethodInfo methodInfo in methodProvider.GetRouteMethods())
				{
					methods.Add(methodInfo);
				}
			}
			return methods;
		}


		/// <summary>
		/// Invokes the method with the specified parameters, returns the result of the method
		/// </summary>
		/// <exception cref="RpcInvalidParametersException">Thrown when conversion of parameters fails or when invoking the method is not compatible with the parameters</exception>
		/// <param name="parameters">List of parameters to invoke the method with</param>
		/// <returns>The result of the invoked method</returns>
		private async Task<object> InvokeAsync(RpcMethodInfo methodInfo, RpcPath path, IServiceProvider serviceProvider)
		{
			object obj = null;
			if (serviceProvider != null)
			{
				//Use service provider (if exists) to create instance
				var objectFactory = ActivatorUtilities.CreateFactory(methodInfo.Method.DeclaringType, new Type[0]);
				obj = objectFactory(serviceProvider, null);
			}
			if (obj == null)
			{
				//Use reflection to create instance if service provider failed or is null
				obj = Activator.CreateInstance(methodInfo.Method.DeclaringType);
			}
			try
			{
				object returnObj = methodInfo.Method.Invoke(obj, methodInfo.ConvertedParameters);

				returnObj = await DefaultRpcInvoker.HandleAsyncResponses(returnObj);

				return returnObj;
			}
			catch (TargetInvocationException ex)
			{
				var routeInfo = new RpcRouteInfo(methodInfo, path);

				//Controller error handling
				RpcErrorFilterAttribute errorFilter = methodInfo.Method.DeclaringType.GetTypeInfo().GetCustomAttribute<RpcErrorFilterAttribute>();
				if (errorFilter != null)
				{
					OnExceptionResult result = errorFilter.OnException(routeInfo, ex.InnerException);
					if (!result.ThrowException)
					{
						return result.ResponseObject;
					}
					if (result.ResponseObject is Exception rEx)
					{
						throw rEx;
					}
				}
				throw new RpcUnknownException("Exception occurred from target method execution.", ex);
			}
			catch (Exception ex)
			{
				throw new RpcInvalidParametersException("Exception from attempting to invoke method. Possibly invalid parameters for method.", ex);
			}
		}

		/// <summary>
		/// Handles/Awaits the result object if it is a async Task
		/// </summary>
		/// <param name="returnObj">The result of a invoked method</param>
		/// <returns>Awaits a Task and returns its result if object is a Task, otherwise returns the same object given</returns>
		private static async Task<object> HandleAsyncResponses(object returnObj)
		{
			Task task = returnObj as Task;
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

		/// <summary>
		/// Converts the object array into the exact types the method needs (e.g. long -> int)
		/// </summary>
		/// <param name="parameters">Array of parameters for the method</param>
		/// <returns>Array of objects with the exact types required by the method</returns>
		private object[] ConvertParameters(MethodInfo method, object[] parameters)
		{
			if (parameters == null || !parameters.Any())
			{
				return new object[0];
			}
			ParameterInfo[] parameterInfoList = method.GetParameters();
			for (int index = 0; index < parameters.Length; index++)
			{
				ParameterInfo parameterInfo = parameterInfoList[index];
				parameters[index] = this.ConvertParameter(parameterInfo.ParameterType, parameters[index]);
			}

			return parameters;
		}

		private object ConvertParameter(Type parameterType, object parameterValue)
		{
			if (parameterValue == null)
			{
				return null;
			}
			//Missing type is for optional parameters
			if (parameterValue is Missing)
			{
				return parameterValue;
			}
			Type nullableType = Nullable.GetUnderlyingType(parameterType);
			if (nullableType != null)
			{
				return this.ConvertParameter(nullableType, parameterValue);
			}
			if (parameterValue is string && parameterType == typeof(Guid))
			{
				Guid.TryParse((string)parameterValue, out Guid guid);
				return guid;
			}
			if (parameterType.GetTypeInfo().IsEnum)
			{
				if (parameterValue is string)
				{
					return Enum.Parse(parameterType, (string)parameterValue);
				}
				else if (parameterValue is long)
				{
					return Enum.ToObject(parameterType, parameterValue);
				}
			}
			if (parameterValue is JObject)
			{
				JsonSerializer jsonSerializer = this.GetJsonSerializer();
				return ((JObject)parameterValue).ToObject(parameterType, jsonSerializer);
			}
			if (parameterValue is JArray)
			{
				JsonSerializer jsonSerializer = this.GetJsonSerializer();
				return ((JArray)parameterValue).ToObject(parameterType, jsonSerializer);
			}
			return Convert.ChangeType(parameterValue, parameterType);
		}

		/// <summary>
		/// Detects if list of parameters matches the method signature
		/// </summary>
		/// <param name="parameterList">Array of parameters for the method</param>
		/// <returns>True if the method signature matches the parameterList, otherwise False</returns>
		private (bool Matches, RpcMethodInfo MethodInfo) HasParameterSignature(MethodInfo method, RpcRequest rpcRequest)
		{
			object[] orignialParameterList;
			if (!rpcRequest.Parameters.HasValue)
			{
				orignialParameterList = new object[0];
			}
			else
			{
				switch (rpcRequest.Parameters.Type)
				{
					case RpcParametersType.Dictionary:
						Dictionary<string, object> parameterMap = rpcRequest.Parameters.DictionaryValue;
						bool canParse = this.TryParseParameterList(method, parameterMap, out orignialParameterList);
						if (!canParse)
						{
							return (false, null);
						}
						break;
					case RpcParametersType.Array:
						orignialParameterList = rpcRequest.Parameters.ArrayValue;
						break;
					default:
						orignialParameterList = new JToken[0];
						break;
				}
			}
			ParameterInfo[] parameterInfoList = method.GetParameters();
			if (orignialParameterList.Length > parameterInfoList.Length)
			{
				return (false, null);
			}
			object[] correctedParameterList = new object[parameterInfoList.Length];

			for (int i = 0; i < orignialParameterList.Length; i++)
			{
				ParameterInfo parameterInfo = parameterInfoList[i];
				object parameter = orignialParameterList[i];
				bool isMatch = this.ParameterMatches(parameterInfo, parameter, out object convertedParameter);
				if (!isMatch)
				{
					return (false, null);
				}
				correctedParameterList[i] = convertedParameter;
			}

			if (orignialParameterList.Length < parameterInfoList.Length)
			{
				//make a new array at the same length with padded 'missing' parameters (if optional)
				for (int i = orignialParameterList.Length; i < parameterInfoList.Length; i++)
				{
					if (!parameterInfoList[i].IsOptional)
					{
						return (false, null);
					}
					correctedParameterList[i] = Type.Missing;
				}
			}

			var rpcMethodInfo = new RpcMethodInfo(method, correctedParameterList, orignialParameterList);
			return (true, rpcMethodInfo);
		}

		/// <summary>
		/// Detects if the request parameter matches the method parameter
		/// </summary>
		/// <param name="parameterInfo">Reflection info about a method parameter</param>
		/// <param name="value">The request's value for the parameter</param>
		/// <returns>True if the request parameter matches the type of the method parameter</returns>
		private bool ParameterMatches(ParameterInfo parameterInfo, object value, out object convertedValue)
		{
			Type parameterType = parameterInfo.ParameterType;

			try
			{
				if (value is JToken tokenValue)
				{
					switch (tokenValue.Type)
					{
						case JTokenType.Array:
							{
								JsonSerializer serializer = this.GetJsonSerializer();
								JArray jArray = (JArray)tokenValue;
								convertedValue = jArray.ToObject(parameterType, serializer);
								return true;
							}
						case JTokenType.Object:
							{
								JsonSerializer serializer = this.GetJsonSerializer();
								JObject jObject = (JObject)tokenValue;
								convertedValue = jObject.ToObject(parameterType, serializer);
								return true;
							}
						default:
							convertedValue = tokenValue.ToObject(parameterType);
							return true;
					}
				}
				else
				{
					convertedValue = value;
					return true;
				}
			}
			catch (Exception ex)
			{
				this.logger?.LogWarning($"Parameter '{parameterInfo.Name}' failed to deserialize: " + ex);
				convertedValue = null;
				return false;
			}
		}


		/// <summary>
		/// Tries to parse the parameter map into an ordered parameter list
		/// </summary>
		/// <param name="parametersMap">Map of parameter name to parameter value</param>
		/// <param name="parameterList">Result of converting the map to an ordered list, null if result is False</param>
		/// <returns>True if the parameters can convert to an ordered list based on the method signature, otherwise Fasle</returns>
		private bool TryParseParameterList(MethodInfo method, Dictionary<string, object> parametersMap, out object[] parameterList)
		{
			ParameterInfo[] parameterInfoList = method.GetParameters();
			parameterList = new object[parameterInfoList.Count()];
			foreach (ParameterInfo parameterInfo in parameterInfoList)
			{
				if (!parametersMap.ContainsKey(parameterInfo.Name) && !parameterInfo.IsOptional)
				{
					parameterList = null;
					return false;
				}
				parameterList[parameterInfo.Position] = parametersMap[parameterInfo.Name];
			}
			return true;
		}

	}
}
