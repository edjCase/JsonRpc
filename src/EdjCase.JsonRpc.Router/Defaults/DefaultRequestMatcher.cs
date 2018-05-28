using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

		public List<RpcMethodInfo> FilterAndBuildMethodInfoByRequest(List<MethodInfo> methods, RpcRequest request)
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
			if (parameterValue is JToken jToken)
			{
				JsonSerializer jsonSerializer = this.GetJsonSerializer();
				return jToken.ToObject(parameterType, jsonSerializer);
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
			parametersMap = parametersMap
				.ToDictionary(x => DefaultRequestMatcher.FixCase(x.Key) ?? x.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
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