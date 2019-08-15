using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Edjcase.JsonRpc.Router;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdjCase.JsonRpc.Router.Defaults
{
	public class DefaultRequestMatcher : IRpcRequestMatcher
	{
		private ILogger<DefaultRequestMatcher> logger { get; }
		private IOptions<RpcServerConfiguration> serverConfig { get; }
		public DefaultRequestMatcher(ILogger<DefaultRequestMatcher> logger,
		IOptions<RpcServerConfiguration> serverConfig)
		{
			this.logger = logger;
			this.serverConfig = serverConfig;
		}

		public RpcMethodInfo GetMatchingMethod(RpcRequest request, List<MethodInfo> methods)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			this.logger?.LogDebug($"Attempting to match Rpc request to a method '{request.Method}'");


			List<RpcMethodInfo> matches = this.FilterAndBuildMethodInfoByRequest(methods, request);


			RpcMethodInfo rpcMethod;
			if (matches.Count > 1)
			{
				var methodInfoList = new List<string>();
				foreach (RpcMethodInfo matchedMethod in matches)
				{
					var parameterTypeList = new List<string>();
					foreach (ParameterInfo parameterInfo in matchedMethod.Method.GetParameters())
					{
						string parameterType = parameterInfo.Name + ": " + parameterInfo.ParameterType.Name;
						if (parameterInfo.IsOptional)
						{
							parameterType += "(Optional)";
						}
						parameterTypeList.Add(parameterType);
					}
					string parameterString = string.Join(", ", parameterTypeList);
					methodInfoList.Add($"{{Name: '{matchedMethod.Method.Name}', Parameters: [{parameterString}]}}");
				}
				string errorMessage = "More than one method matched the rpc request. Unable to invoke due to ambiguity. Methods that matched the same name: " + string.Join(", ", methodInfoList);
				this.logger?.LogError(errorMessage);
				throw new RpcException(RpcErrorCode.MethodNotFound, errorMessage);
			}
			else if (matches.Count == 0)
			{
				//Log diagnostics 
				string methodsString = string.Join(", ", methods.Select(m => m.Name));
				this.logger?.LogTrace("Methods in route: " + methodsString);

				const string errorMessage = "No methods matched request.";
				this.logger?.LogError(errorMessage);
				throw new RpcException(RpcErrorCode.MethodNotFound, errorMessage);
			}
			else
			{
				rpcMethod = matches.First();
			}
			this.logger?.LogDebug("Request was matched to a method");
			return rpcMethod;
		}


		private List<RpcMethodInfo> FilterAndBuildMethodInfoByRequest(List<MethodInfo> methods, RpcRequest request)
		{
			//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
			List<MethodInfo> methodsWithSameName = methods
				.Where(m => string.Equals(m.Name, request.Method, StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (!methodsWithSameName.Any())
			{
				string methodName = DefaultRequestMatcher.FixCase(request.Method);

				if (methodName != null)
				{
					methodsWithSameName = methods
						.Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
						.ToList();
				}
			}
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
				}
			}

			return potentialMatches;


		}

		private static string FixCase(string method)
		{
			//Snake case
			if (method.Contains('_'))
			{
				return method
					.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
					.Aggregate((s1, s2) => s1 + s2);
			}
			//Spinal case
			else if (method.Contains('-'))
			{
				return method
					.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
					.Aggregate((s1, s2) => s1 + s2);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Detects if list of parameters matches the method signature
		/// </summary>
		/// <param name="parameterList">Array of parameters for the method</param>
		/// <returns>True if the method signature matches the parameterList, otherwise False</returns>
		private (bool Matches, RpcMethodInfo MethodInfo) HasParameterSignature(MethodInfo method, RpcRequest rpcRequest)
		{
			IRpcParameter[] parameters;
			if (rpcRequest.Parameters == null)
			{
				parameters = new IRpcParameter[0];
			}
			else
			{
				if (rpcRequest.Parameters.IsDictionary)
				{
					Dictionary<string, IRpcParameter> parameterMap = rpcRequest.Parameters.AsDictionary;
					bool canParse = this.TryParseParameterList(method, parameterMap, out parameters);
					if (!canParse)
					{
						return (false, null);
					}
				}
				else
				{
					parameters = rpcRequest.Parameters.AsList.ToArray();
				}
			}
			ParameterInfo[] parameterInfoList = method.GetParameters();
			if (parameters.Length > parameterInfoList.Length)
			{
				return (false, null);
			}

			object[] deserializedParameters = new object[parameterInfoList.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameterInfo = parameterInfoList[i];
				IRpcParameter parameter = parameters[i];
				if (!parameter.TryGetValue(parameterInfo.ParameterType, out object value))
				{
					return (false, null);
				}
				deserializedParameters[i] = value;
			}

			var rpcMethodInfo = new RpcMethodInfo(method, deserializedParameters);
			return (true, rpcMethodInfo);
		}


		/// <summary>
		/// Tries to parse the parameter map into an ordered parameter list
		/// </summary>
		/// <param name="parametersMap">Map of parameter name to parameter value</param>
		/// <param name="parameterList">Result of converting the map to an ordered list, null if result is False</param>
		/// <returns>True if the parameters can convert to an ordered list based on the method signature, otherwise Fasle</returns>
		private bool TryParseParameterList(MethodInfo method, Dictionary<string, IRpcParameter> parametersMap, out IRpcParameter[] parameterList)
		{
			parametersMap = parametersMap
				.ToDictionary(x => DefaultRequestMatcher.FixCase(x.Key) ?? x.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
			ParameterInfo[] parameterInfoList = method.GetParameters();
			parameterList = new IRpcParameter[parameterInfoList.Count()];
			foreach (ParameterInfo parameterInfo in parameterInfoList)
			{
				if (!parametersMap.ContainsKey(parameterInfo.Name))
				{
					if (!parameterInfo.IsOptional)
					{
						parameterList = null;
						return false;
					}
				}
				else
				{
					parameterList[parameterInfo.Position] = parametersMap[parameterInfo.Name];
				}
			}
			return true;
		}

	}
}