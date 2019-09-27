using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Edjcase.JsonRpc.Router;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdjCase.JsonRpc.Router.Defaults
{
	public class RpcEndpointInfo
	{
		public Dictionary<RpcPath, List<MethodInfo>> Routes { get; }

		public RpcEndpointInfo(Dictionary<RpcPath, List<MethodInfo>> routes)
		{
			this.Routes = routes ?? throw new ArgumentNullException(nameof(routes));
		}
	}


	public class DefaultRequestMatcher : IRpcRequestMatcher
	{
		private static ConcurrentDictionary<MethodInfo, CompiledMethodInfo> compiledMethodCache { get; } = new ConcurrentDictionary<MethodInfo, CompiledMethodInfo>();
		private static ConcurrentDictionary<string, CompiledMethodInfo> requestToMethodCache { get; } = new ConcurrentDictionary<string, CompiledMethodInfo>();

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
			this.logger.LogDebug($"Attempting to match Rpc request to a method '{request.Method}'");

			CompiledMethodInfo[] compiledMethods = ArrayPool<CompiledMethodInfo>.Shared.Rent(methods.Count);
			Span<Router.RpcMethodInfo> matches;
			try
			{
				this.CompileMethodInfo(methods, compiledMethods);
				matches = this.FilterAndBuildMethodInfoByRequest(compiledMethods, request);
			}
			finally
			{
				ArrayPool<CompiledMethodInfo>.Shared.Return(compiledMethods, clearArray: true);
			}
			if (matches.Length == 1)
			{
				this.logger.LogDebug("Request was matched to a method");
				return matches[0];
			}

			string errorMessage;
			if (matches.Length > 1)
			{
				var methodInfoList = new List<string>();
				foreach (Router.RpcMethodInfo matchedMethod in matches)
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
				errorMessage = "More than one method matched the rpc request. Unable to invoke due to ambiguity. Methods that matched the same name: " + string.Join(", ", methodInfoList);
			}
			else
			{
				//Log diagnostics 
				string methodsString = string.Join(", ", methods.Select(m => m.Name));
				this.logger.LogTrace("Methods in route: " + methodsString);
				errorMessage = "No methods matched request.";
			}
			this.logger.LogError(errorMessage);
			throw new RpcException(RpcErrorCode.MethodNotFound, errorMessage);
		}

		private void CompileMethodInfo(List<MethodInfo> methods, CompiledMethodInfo[] compiledMethods)
		{
			for (int i = 0; i < methods.Count; i++)
			{
				MethodInfo methodInfo = methods[i];
				if (!DefaultRequestMatcher.compiledMethodCache.TryGetValue(methodInfo, out CompiledMethodInfo info))
				{
					var parameters = methodInfo.GetParameters().Select(ExtractParam).ToArray();
					info = new CompiledMethodInfo(methodInfo, parameters);

					CompiledParameterInfo ExtractParam(ParameterInfo parameterInfo)
					{
						RpcParameterType type;
						if (parameterInfo.ParameterType == typeof(short)
							|| parameterInfo.ParameterType == typeof(ushort)
							|| parameterInfo.ParameterType == typeof(int)
							|| parameterInfo.ParameterType == typeof(uint)
							|| parameterInfo.ParameterType == typeof(long)
							|| parameterInfo.ParameterType == typeof(ulong)
							|| parameterInfo.ParameterType == typeof(float)
							|| parameterInfo.ParameterType == typeof(double)
							|| parameterInfo.ParameterType == typeof(decimal))
						{
							type = RpcParameterType.Number;
						}
						else if (parameterInfo.ParameterType == typeof(string))
						{
							type = RpcParameterType.String;
						}
						else
						{
							type = RpcParameterType.Object;
						}
						return new CompiledParameterInfo(parameterInfo.Name, type, parameterInfo.ParameterType, parameterInfo.IsOptional);
					}
				}
				compiledMethods[i] = info;
			}
		}

		private Span<Router.RpcMethodInfo> FilterAndBuildMethodInfoByRequest(IReadOnlyList<CompiledMethodInfo> methods, RpcRequest request)
		{
			string requestSignature = request.GetSignature();
			if (this.TryGetMethodBySignature(requestSignature, out Span<Router.RpcMethodInfo> cachedMethod))
			{
				return cachedMethod;
			}

			CompiledMethodInfo[] methodsWithSameName = ArrayPool<CompiledMethodInfo>.Shared.Rent(methods.Count);
			try
			{
				//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
				int methodsWithSameNameCount = 0;
				for (int i = 0; i < methods.Count(); i++)
				{
					CompiledMethodInfo compiledMethodInfo = methods[i];
					if (RpcUtil.NamesMatch(compiledMethodInfo.MethodInfo.Name.AsSpan(), request.Method.AsSpan()))
					{
						methodsWithSameName[methodsWithSameNameCount++] = compiledMethodInfo;
						break;
					}
				}
				if (methodsWithSameNameCount < 1)
				{
					return Span<Router.RpcMethodInfo>.Empty;
				}

				Router.RpcMethodInfo[] potentialMatches = ArrayPool<Router.RpcMethodInfo>.Shared.Rent(methodsWithSameNameCount);

				try
				{
					int potentialMatchCount = 0;
					foreach (CompiledMethodInfo m in methodsWithSameName)
					{
						(bool isMatch, Router.RpcMethodInfo methodInfo) = this.HasParameterSignature(m, request);
						if (isMatch)
						{
							potentialMatches[potentialMatchCount++] = methodInfo;
						}
					}

					if (potentialMatchCount <= 1)
					{
						return potentialMatches.AsSpan(0, potentialMatchCount);
					}
					Router.RpcMethodInfo[] exactParamMatches = ArrayPool<Router.RpcMethodInfo>.Shared.Rent(potentialMatchCount);
					try
					{
						int exactParamMatchCount = 0;
						//Try to remove ambiguity with 'perfect matching' (usually optional params and types)
						for (int i = 0; i < potentialMatchCount; i++)
						{
							bool matched = true;
							Router.RpcMethodInfo info = potentialMatches[i];
							ParameterInfo[] parameters = info.Method.GetParameters();
							if (info.Parameters.Length == parameters.Length)
							{
								for (int j = 0; j < info.Parameters.Length; j++)
								{
									object original = info.Parameters[j];
									ParameterInfo parameter = parameters[j];
									if (!RpcUtil.TypesMatch(original, parameter.ParameterType))
									{
										matched = false;
										break;
									}
								}
							}
							else
							{
								matched = false;
							}
							if (matched)
							{
								exactParamMatches[exactParamMatchCount++] = potentialMatches[i];
							}
						}

						(Router.RpcMethodInfo[] matches, int count) = exactParamMatchCount > 0
							? (exactParamMatches, exactParamMatchCount)
							: (potentialMatches, potentialMatchCount);

						if (count <= 1)
						{
							return potentialMatches.AsSpan(0, count);
						}
						//Try to remove ambiguity with case sensitive check
						Router.RpcMethodInfo[] caseSensitiveMatches = ArrayPool<Router.RpcMethodInfo>.Shared.Rent(count);
						try
						{
							int caseSensitiveCount = 0;
							for (int i = 0; i < count; i++)
							{
								Router.RpcMethodInfo m = matches[i];
								if (string.Equals(m.Method.Name, request.Method, StringComparison.Ordinal))
								{
									caseSensitiveMatches[caseSensitiveCount++] = m;
								}
							}
							return caseSensitiveMatches.AsSpan(0, caseSensitiveCount);
						}
						finally
						{
							ArrayPool<Router.RpcMethodInfo>.Shared.Return(caseSensitiveMatches, clearArray: false);
						}
					}
					finally
					{
						ArrayPool<Router.RpcMethodInfo>.Shared.Return(exactParamMatches, clearArray: false);
					}
				}
				finally
				{
					ArrayPool<Router.RpcMethodInfo>.Shared.Return(potentialMatches, clearArray: false);
				}
			}
			finally
			{
				ArrayPool<CompiledMethodInfo>.Shared.Return(methodsWithSameName, clearArray: false);
			}
		}

		private bool TryGetMethodBySignature(string requestSignature, out Span<RpcMethodInfo> cachedMethod)
		{

		}


		/// <summary>
		/// Detects if list of parameters matches the method signature
		/// </summary>
		/// <param name="parameterList">Array of parameters for the method</param>
		/// <returns>True if the method signature matches the parameterList, otherwise False</returns>
		private (bool Matches, Router.RpcMethodInfo MethodInfo) HasParameterSignature(CompiledMethodInfo method, RpcRequest rpcRequest)
		{
			IList<IRpcParameter> parameters;
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
					parameters = rpcRequest.Parameters.AsList;
				}
			}
			if (parameters.Count() > method.Parameters.Length)
			{
				return (false, null);
			}

			for (int i = 0; i < parameters.Count(); i++)
			{
				CompiledParameterInfo parameterInfo = method.Parameters[i];
				IRpcParameter parameter = parameters[i];
				if (parameter.Type != parameterInfo.Type)
				{
					return (false, null);
				}
			}

			object[] deserializedParameters = new object[method.Parameters.Length];
			for (int i = 0; i < parameters.Count(); i++)
			{
				CompiledParameterInfo parameterInfo = method.Parameters[i];
				IRpcParameter parameter = parameters[i];
				if (!parameter.TryGetValue(parameterInfo.RawType, out object value))
				{
					return (false, null);
				}
				deserializedParameters[i] = value;
			}

			var rpcMethodInfo = new Router.RpcMethodInfo(method.MethodInfo, deserializedParameters);
			return (true, rpcMethodInfo);
		}


		private bool TryParseParameterList(CompiledMethodInfo method, Dictionary<string, IRpcParameter> requestParameters, out IList<IRpcParameter> parameterList)
		{
			parameterList = new IRpcParameter[method.Parameters.Count()];
			for (int i = 0; i < method.Parameters.Length; i++)
			{
				CompiledParameterInfo parameterInfo = method.Parameters[i];

				foreach (KeyValuePair<string, IRpcParameter> requestParameter in requestParameters)
				{
					if (RpcUtil.NamesMatch(parameterInfo.Name.AsSpan(), requestParameter.Key.AsSpan()))
					{
						parameterList[i] = requestParameter.Value;
						continue;
					}
				}
				if (!parameterInfo.IsOptional)
				{
					parameterList = null;
					return false;
				}
			}
			return true;
		}


		private class CompiledMethodInfo
		{
			public MethodInfo MethodInfo { get; }
			public CompiledParameterInfo[] Parameters { get; }

			public CompiledMethodInfo(MethodInfo methodInfo, CompiledParameterInfo[] parameters)
			{
				this.MethodInfo = methodInfo;
				this.Parameters = parameters;
			}
		}

		private class CompiledParameterInfo
		{
			public string Name { get; }
			public RpcParameterType Type { get; }
			public Type RawType { get; }
			public bool IsOptional { get; }

			public CompiledParameterInfo(string name, RpcParameterType type, Type rawType, bool isOptional)
			{
				this.Name = name;
				this.Type = type;
				this.RawType = rawType;
				this.IsOptional = isOptional;
			}
		}

	}
}