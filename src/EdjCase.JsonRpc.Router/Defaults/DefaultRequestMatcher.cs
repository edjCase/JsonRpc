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
using Microsoft.Extensions.Caching.Memory;

namespace EdjCase.JsonRpc.Router.Defaults
{
	internal class RpcEndpointInfo
	{
		public Dictionary<RpcPath, List<MethodInfo>> Routes { get; }

		public RpcEndpointInfo(Dictionary<RpcPath, List<MethodInfo>> routes)
		{
			this.Routes = routes ?? throw new ArgumentNullException(nameof(routes));
		}
	}

	internal class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
	{
		public static MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer();

		public bool Equals(MethodInfo? x, MethodInfo? y)
		{
			if (x == null || y == null)
			{
				return x == null && y == null;
			}
			if (object.ReferenceEquals(x, y))
			{
				return true;
			}
			if (x.Name != y.Name)
			{
				return false;
			}
			if (x.DeclaringType != y.DeclaringType)
			{
				return false;
			}
			ParameterInfo[] xParameters = x.GetParameters();
			ParameterInfo[] yParameters = y.GetParameters();
			if (!xParameters.SequenceEqual(yParameters, ParameterInfoEqualityComparer.Instance))
			{
				return false;
			}
			return true;
		}

		public int GetHashCode(MethodInfo obj)
		{
			return obj.Name.GetHashCode();
		}
	}

	internal class ParameterInfoEqualityComparer : IEqualityComparer<ParameterInfo>
	{
		public static ParameterInfoEqualityComparer Instance = new ParameterInfoEqualityComparer();
		public bool Equals(ParameterInfo? x, ParameterInfo? y)
		{
			if (x == null || y == null)
			{
				return x == null && y == null;
			}
			if (x.ParameterType != y.ParameterType)
			{
				return false;
			}
			if (x.Name != y.Name)
			{
				return false;
			}
			return true;
		}

		public int GetHashCode(ParameterInfo obj)
		{
			return obj.Name?.GetHashCode() ?? 0;
		}
	}

	internal class DefaultRequestMatcher : IRpcRequestMatcher
	{
		private static ConcurrentDictionary<RpcRequestSignature, IRpcMethodInfo[]> requestToMethodCache { get; } = new ConcurrentDictionary<RpcRequestSignature, IRpcMethodInfo[]>();

		private ILogger<DefaultRequestMatcher> logger { get; }
		private IRpcMethodProvider methodProvider { get; }
		private IRpcContextAccessor contextAccessor { get; }
		public DefaultRequestMatcher(ILogger<DefaultRequestMatcher> logger,
			IRpcMethodProvider methodProvider,
			IRpcContextAccessor contextAccessor)
		{
			this.logger = logger;
			this.methodProvider = methodProvider;
			this.contextAccessor = contextAccessor;
		}

		public IRpcMethodInfo GetMatchingMethod(RpcRequestSignature requestSignature)
		{
			this.logger.AttemptingToMatchMethod(new string(requestSignature.GetMethodName().Span));

			RpcContext context = this.contextAccessor.Get();
			IReadOnlyList<IRpcMethodInfo> methods = this.methodProvider.GetByPath(context.Path);
			if (methods == null || !methods.Any())
			{
				throw new RpcException(RpcErrorCode.MethodNotFound, $"No methods found for route");
			}

			Span<IRpcMethodInfo> matches = this.FilterAndBuildMethodInfoByRequest(methods, requestSignature);
			if (matches.Length == 1)
			{
				this.logger.RequestMatchedMethod();
				return matches[0];
			}

			string errorMessage;
			if (matches.Length > 1)
			{
				var methodInfoList = new List<string>();
				foreach (IRpcMethodInfo matchedMethod in matches)
				{
					var parameterTypeList = new List<string>();
					foreach (IRpcParameterInfo parameterInfo in matchedMethod.Parameters)
					{
						string parameterType = parameterInfo.Name + ": " + parameterInfo.Type;
						if (parameterInfo.IsOptional)
						{
							parameterType += "(Optional)";
						}
						parameterTypeList.Add(parameterType);
					}
					string parameterString = string.Join(", ", parameterTypeList);
					methodInfoList.Add($"{{Name: '{matchedMethod.Name}', Parameters: [{parameterString}]}}");
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

		private IRpcMethodInfo[] FilterAndBuildMethodInfoByRequest(IReadOnlyList<IRpcMethodInfo> methods, RpcRequestSignature requestSignature)
		{
			//If the request signature is found, it means we have the methods cached already
			return DefaultRequestMatcher.requestToMethodCache.GetOrAdd(requestSignature, GetMatchingMethodsWrapper);

			IRpcMethodInfo[] GetMatchingMethodsWrapper(RpcRequestSignature s)
			{
				return this.GetMatchingMethods(s, methods);
			}
		}


		private IRpcMethodInfo[] GetMatchingMethods(RpcRequestSignature requestSignature, IReadOnlyList<IRpcMethodInfo> methods)
		{
			IRpcMethodInfo[] methodsWithSameName = ArrayPool<IRpcMethodInfo>.Shared.Rent(methods.Count);
			try
			{
				//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
				int methodsWithSameNameCount = 0;
				for (int i = 0; i < methods.Count; i++)
				{
					IRpcMethodInfo methodInfo = methods[i];
					if (RpcUtil.NamesMatch(methodInfo.Name.AsSpan(), requestSignature.GetMethodName().Span))
					{
						methodsWithSameName[methodsWithSameNameCount++] = methodInfo;
					}
				}

				if (methodsWithSameNameCount < 1)
				{
					return Array.Empty<IRpcMethodInfo>();
				}
				return this.FilterBySimilarParams(requestSignature, methodsWithSameName.AsSpan(0, methodsWithSameNameCount));
			}
			finally
			{
				ArrayPool<IRpcMethodInfo>.Shared.Return(methodsWithSameName, clearArray: false);
			}
		}

		private IRpcMethodInfo[] FilterBySimilarParams(RpcRequestSignature requestSignature, Span<IRpcMethodInfo> methodsWithSameName)
		{
			IRpcMethodInfo[] potentialMatches = ArrayPool<IRpcMethodInfo>.Shared.Rent(methodsWithSameName.Length);

			try
			{
				int potentialMatchCount = 0;
				for (int i = 0; i < methodsWithSameName.Length; i++)
				{
					IRpcMethodInfo m = methodsWithSameName[i];

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
				return this.FilterMatchesByCaseSensitiveMethod(requestSignature, potentialMatches.AsSpan(0, potentialMatchCount));
			}
			finally
			{
				ArrayPool<IRpcMethodInfo>.Shared.Return(potentialMatches, clearArray: false);
			}
		}

		private IRpcMethodInfo[] FilterMatchesByCaseSensitiveMethod(RpcRequestSignature requestSignature, Span<IRpcMethodInfo> matches)
		{
			//Try to remove ambiguity with case sensitive check
			IRpcMethodInfo[] caseSensitiveMatches = ArrayPool<IRpcMethodInfo>.Shared.Rent(matches.Length);
			try
			{
				int caseSensitiveCount = 0;
				for (int i = 0; i < matches.Length; i++)
				{
					IRpcMethodInfo m = matches[i];
					Memory<char> requestMethodName = requestSignature.GetMethodName();
					if (m.Name.Length == requestMethodName.Length)
					{
						if (!RpcUtil.NamesMatch(m.Name.AsSpan(), requestMethodName.Span))
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
				ArrayPool<IRpcMethodInfo>.Shared.Return(caseSensitiveMatches, clearArray: false);
			}
		}

		private bool ParametersMatch(RpcRequestSignature requestSignature, IReadOnlyList<IRpcParameterInfo> parameters)
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
					bool found = false;
					for (int paramIndex = 0; paramIndex < parameters.Count; paramIndex++)
					{
						IRpcParameterInfo parameter = parameters[paramIndex];
						if (!RpcUtil.NamesMatch(parameter.Name.AsSpan(), name.Span) ||
							!RpcParameterUtil.TypesCompatible(parameter.Type, type))
						{
							continue;
						}
						found = true;
						break;
					}
					if (!found)
					{
						return false;
					}
					parameterCount++;
				}
			}
			else
			{
				foreach (RpcParameterType parameterType in requestSignature.ParametersAsList)
				{
					if (parameters.Count <= parameterCount)
					{
						return false;
					}
					IRpcParameterInfo info = parameters[parameterCount];
					if (!RpcParameterUtil.TypesCompatible(info.Type, parameterType))
					{
						return false;
					}

					parameterCount++;
				}


				for (int i = parameterCount; i < parameters.Count; i++)
				{
					//Only if the last parameters in the method are optional does the request match
					//Will be skipped if they are equal length
					if (!parameters[i].IsOptional)
					{
						return false;
					}
				}
			}
			if (parameterCount != parameters.Count)
			{
				return false;
			}
			return true;

		}

	}
}