using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common;
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
		private static ConcurrentDictionary<MethodInfo, RpcMethodInfo> compiledMethodCache { get; } = new ConcurrentDictionary<MethodInfo, RpcMethodInfo>();
		private static ConcurrentDictionary<string, RpcMethodInfo[]> requestToMethodCache { get; } = new ConcurrentDictionary<string, RpcMethodInfo[]>();

		private ILogger<DefaultRequestMatcher> logger { get; }
		private IRpcContextAccessor routeContextAccessor { get; }
		private IRpcMethodProvider methodProvider { get; }
		public DefaultRequestMatcher(ILogger<DefaultRequestMatcher> logger,
			IRpcContextAccessor routeContextAccessor,
			IRpcMethodProvider methodProvider)
		{
			this.logger = logger;
			this.routeContextAccessor = routeContextAccessor;
			this.methodProvider = methodProvider;
		}

		public RpcMethodInfo GetMatchingMethod(RpcRequestSignature requestSignature)
		{
			//TODo avoid .ToString() here
			this.logger.AttemptingToMatchMethod(requestSignature.GetMethodName().ToString());

			IRpcContext routeContext = this.routeContextAccessor.Value;
			IReadOnlyList<MethodInfo> methods = this.methodProvider.Get();
			if (methods == null || !methods.Any())
			{
				throw new RpcException(RpcErrorCode.MethodNotFound, $"No methods found for route");
			}

			RpcMethodInfo[] compiledMethods = ArrayPool<RpcMethodInfo>.Shared.Rent(methods.Count);
			Span<RpcMethodInfo> matches;
			try
			{
				this.FillRpcMethodInfos(methods, compiledMethods);
				matches = this.FilterAndBuildMethodInfoByRequest(compiledMethods.AsMemory(0, methods.Count), requestSignature);
			}
			finally
			{
				ArrayPool<RpcMethodInfo>.Shared.Return(compiledMethods, clearArray: true);
			}
			if (matches.Length == 1)
			{
				this.logger.RequestMatchedMethod();
				return matches[0];
			}

			string errorMessage;
			if (matches.Length > 1)
			{
				var methodInfoList = new List<string>();
				foreach (RpcMethodInfo matchedMethod in matches)
				{
					var parameterTypeList = new List<string>();
					foreach (ParameterInfo parameterInfo in matchedMethod.MethodInfo.GetParameters())
					{
						string parameterType = parameterInfo.Name + ": " + parameterInfo.ParameterType.Name;
						if (parameterInfo.IsOptional)
						{
							parameterType += "(Optional)";
						}
						parameterTypeList.Add(parameterType);
					}
					string parameterString = string.Join(", ", parameterTypeList);
					methodInfoList.Add($"{{Name: '{matchedMethod.MethodInfo.Name}', Parameters: [{parameterString}]}}");
				}
				errorMessage = "More than one method matched the rpc request. Unable to invoke due to ambiguity. Methods that matched the same name: " + string.Join(", ", methodInfoList);
			}
			else
			{
				//Log diagnostics 
				this.logger.MethodsInRoute(methods);
				errorMessage = "No methods matched request.";
			}
			throw new RpcException(RpcErrorCode.MethodNotFound, errorMessage);
		}

		private void FillRpcMethodInfos(IReadOnlyList<MethodInfo> methods, RpcMethodInfo[] compiledMethods)
		{
			for (int i = 0; i < methods.Count; i++)
			{
				MethodInfo methodInfo = methods[i];
				if (!DefaultRequestMatcher.compiledMethodCache.TryGetValue(methodInfo, out RpcMethodInfo info))
				{
					info = DefaultRequestMatcher.BuildMethodInfo(methodInfo);
				}
				compiledMethods[i] = info;
			}
		}

		public static RpcMethodInfo BuildMethodInfo(MethodInfo methodInfo)
		{
			RpcParameterInfo[] parameters = methodInfo
				.GetParameters()
				.Select(ExtractParam)
				.ToArray();
			return new RpcMethodInfo(methodInfo, parameters);

			RpcParameterInfo ExtractParam(ParameterInfo parameterInfo)
			{
				Type parameterType = parameterInfo.ParameterType;
				if (parameterType.IsGenericType)
				{
					Type realType = Nullable.GetUnderlyingType(parameterType);
					if (realType != null)
					{
						parameterType = realType;
					}
				}
				RpcParameterType type;
				if (parameterType == typeof(short)
					|| parameterType == typeof(ushort)
					|| parameterType == typeof(int)
					|| parameterType == typeof(uint)
					|| parameterType == typeof(long)
					|| parameterType == typeof(ulong)
					|| parameterType == typeof(float)
					|| parameterType == typeof(double)
					|| parameterType == typeof(decimal))
				{
					type = RpcParameterType.Number;
				}
				else if (parameterType == typeof(string))
				{
					type = RpcParameterType.String;
				}
				else if (parameterType == typeof(bool))
				{
					type = RpcParameterType.Boolean;
				}
				else
				{
					type = RpcParameterType.Object;
				}
				return new RpcParameterInfo(parameterInfo.Name, type, parameterInfo.ParameterType, parameterInfo.IsOptional);
			}
		}

		private RpcMethodInfo[] FilterAndBuildMethodInfoByRequest(Memory<RpcMethodInfo> methods, RpcRequestSignature requestSignature)
		{
			//If the request signature is found, it means we have the methods cached already
			//TODO make a cache that uses spans/char array and not strings
			//TODO does the entire method info need to be cached?
			return DefaultRequestMatcher.requestToMethodCache.GetOrAdd(requestSignature.AsString(), BuildCache);

			RpcMethodInfo[] BuildCache(string s)
			{
				return this.BuildMethodInfos(requestSignature, methods.Span);
			}
		}


		private RpcMethodInfo[] BuildMethodInfos(RpcRequestSignature requestSignature, Span<RpcMethodInfo> methods)
		{
			RpcMethodInfo[] methodsWithSameName = ArrayPool<RpcMethodInfo>.Shared.Rent(methods.Length);
			try
			{
				//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
				int methodsWithSameNameCount = 0;
				for (int i = 0; i < methods.Length; i++)
				{
					RpcMethodInfo compiledMethodInfo = methods[i];
					if (RpcUtil.NamesMatch(compiledMethodInfo.MethodInfo.Name.AsSpan(), requestSignature.GetMethodName().Span))
					{
						methodsWithSameName[methodsWithSameNameCount++] = compiledMethodInfo;
					}
				}

				if (methodsWithSameNameCount < 1)
				{
					return Array.Empty<RpcMethodInfo>();
				}
				return this.FilterBySimilarParams(requestSignature, methodsWithSameName.AsSpan(0, methodsWithSameNameCount));
			}
			finally
			{
				ArrayPool<RpcMethodInfo>.Shared.Return(methodsWithSameName, clearArray: false);
			}
		}

		private RpcMethodInfo[] FilterBySimilarParams(RpcRequestSignature requestSignature, Span<RpcMethodInfo> methodsWithSameName)
		{
			RpcMethodInfo[] potentialMatches = ArrayPool<RpcMethodInfo>.Shared.Rent(methodsWithSameName.Length);

			try
			{
				int potentialMatchCount = 0;
				for (int i = 0; i < methodsWithSameName.Length; i++)
				{
					RpcMethodInfo m = methodsWithSameName[i];

					bool isMatch = this.ParametersMatch(requestSignature, m.Parameters);
					if (isMatch)
					{
						potentialMatches[potentialMatchCount++] = m;
					}
				}

				if (potentialMatchCount <= 1)
				{
					return potentialMatches.AsSpan(0, potentialMatchCount).ToArray();
				}
				return this.FilterByExactParams(requestSignature, potentialMatches.AsSpan(0, potentialMatchCount));
			}
			finally
			{
				ArrayPool<RpcMethodInfo>.Shared.Return(potentialMatches, clearArray: false);
			}
		}


		public bool ParametersMatch(RpcRequestSignature requestSignature, RpcParameterInfo[] parameters)
		{
			if (!requestSignature.HasParameters)
			{
				return parameters == null || !parameters.Any(p => !p.IsOptional);
			}
			int parameterCount = 0;
			if (requestSignature.IsDictionary)
			{
				foreach ((Memory<char> name, RpcParameterType type) in requestSignature.ParametersAsDict)
				{
					//TODO use a better matching system?
					foreach (RpcParameterInfo parameter in parameters)
					{
						if(parameter.Name.Length != name.Length)
						{
							return false;
						}
						for (int i = 0; i < name.Length; i++)
						{
							//TODO this needs to allow different cases?
							if (parameter.Name.Length <= i
								|| name.Span[i] != parameter.Name[i])
							{
								//Name doesnt match
								return false;
							}
						}
						if (!RpcParameterUtil.TypesCompatible(type, parameter.Type))
						{
							return false;
						}
					}
					parameterCount++;
				}
			}
			else
			{
				foreach (RpcParameterType parameterType in requestSignature.ParametersAsList)
				{
					//TODO use a better matching system?
					foreach (RpcParameterInfo parameter in parameters)
					{
						if (!RpcParameterUtil.TypesCompatible(parameterType, parameter.Type))
						{
							return false;
						}
					}
					parameterCount++;
				}

				
				for (int i = parameterCount; i >= parameters.Length; i++)
				{
					//Only if the last parameters in the method are optional does the request match
					//Will be skipped if they are equal length
					if (!parameters[i].IsOptional)
					{
						return false;
					}
				}
			}
			if (parameterCount != parameters.Length)
			{
				return false;
			}
			return true;

		}


		private RpcMethodInfo[] FilterByExactParams(RpcRequestSignature requestSignature, Span<RpcMethodInfo> potentialMatches)
		{
			RpcMethodInfo[] exactParamMatches = ArrayPool<RpcMethodInfo>.Shared.Rent(potentialMatches.Length);
			try
			{
				int exactParamMatchCount = 0;
				//Try to remove ambiguity with 'perfect matching' (usually optional params and types)
				for (int i = 0; i < potentialMatches.Length; i++)
				{
					bool matched = true;
					RpcMethodInfo info = potentialMatches[i];
					ParameterInfo[] parameters = info.MethodInfo.GetParameters();
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

				//Fallback to not-exact match if there are none found
				Span<RpcMethodInfo> matches = exactParamMatchCount > 0 ? exactParamMatches.AsSpan(0, exactParamMatchCount) : potentialMatches;

				if (matches.Length <= 1)
				{
					return matches.ToArray();
				}
				return this.FilterMatchesByCaseSensitiveMethod(requestSignature, matches);
			}
			finally
			{
				ArrayPool<RpcMethodInfo>.Shared.Return(exactParamMatches, clearArray: false);
			}
		}

		private RpcMethodInfo[] FilterMatchesByCaseSensitiveMethod(RpcRequestSignature requestSignature, Span<RpcMethodInfo> matches)
		{
			//Try to remove ambiguity with case sensitive check
			RpcMethodInfo[] caseSensitiveMatches = ArrayPool<RpcMethodInfo>.Shared.Rent(matches.Length);
			try
			{
				int caseSensitiveCount = 0;
				for (int i = 0; i < matches.Length; i++)
				{
					RpcMethodInfo m = matches[i];
					Memory<char> requestMethodName = requestSignature.GetMethodName();
					if (m.MethodInfo.Name.Length == requestMethodName.Length)
					{
						if (!RpcUtil.NamesMatch(m.MethodInfo.Name.AsSpan(), requestMethodName.Span))
						{
							//TODO do we care about the case where 2+ parameters have very similar names and types?
							continue;
						}
						caseSensitiveMatches[caseSensitiveCount++] = m;
					}
				}
				return caseSensitiveMatches.AsSpan(0, caseSensitiveCount).ToArray();
			}
			finally
			{
				ArrayPool<RpcMethodInfo>.Shared.Return(caseSensitiveMatches, clearArray: false);
			}
		}
	}
}