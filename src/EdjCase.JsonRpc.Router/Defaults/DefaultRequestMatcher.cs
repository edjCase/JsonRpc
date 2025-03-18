using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace EdjCase.JsonRpc.Router.Defaults
{

	internal class DefaultRequestMatcher : IRpcRequestMatcher
	{
		private ILogger<DefaultRequestMatcher> logger { get; }
		private IRpcMethodProvider methodProvider { get; }
		private IRpcContextAccessor contextAccessor { get; }
		private IRpcParameterConverter rpcParameterConverter { get; }
		private RequestMatcherCache requestMatcherCache { get; }
		public DefaultRequestMatcher(
			ILogger<DefaultRequestMatcher> logger,
			IRpcMethodProvider methodProvider,
			IRpcContextAccessor contextAccessor,
			IRpcParameterConverter rpcParameterConverter,
			RequestMatcherCache requestMatcherCache
		)
		{
			this.contextAccessor = contextAccessor;
			this.logger = logger;
			this.methodProvider = methodProvider;
			this.rpcParameterConverter = rpcParameterConverter;
			this.requestMatcherCache = requestMatcherCache;
		}


		public IRpcMethodInfo GetMatchingMethod(RpcRequestSignature requestSignature)
		{
			this.logger.AttemptingToMatchMethod(new string(requestSignature.GetMethodName().Span));

			// Create efficient cache key from path and signature
			string path = this.contextAccessor.Get().Path?.ToString() ?? string.Empty;

			IRpcMethodInfo[] matchingMethods = this.requestMatcherCache.GetOrAdd(
				path,
				requestSignature,
				() =>
				{
					RpcContext context = this.contextAccessor.Get();
					IReadOnlyList<IRpcMethodInfo>? methods = this.methodProvider.GetByPath(context.Path);
					if (methods == null || !methods.Any())
					{
						return Array.Empty<IRpcMethodInfo>();
					}

					return this.GetMatchingMethods(requestSignature, methods);
				});


			return this.HandleMatchResult(matchingMethods);
		}

		private IRpcMethodInfo HandleMatchResult(IRpcMethodInfo[] matches)
		{
			if (matches.Length == 1)
			{
				this.logger.RequestMatchedMethod();
				return matches[0];
			}
			if (matches.Length > 1)
			{
				// Format the methods for error message
				string errorMessage = "More than one method matched the rpc request. Unable to invoke due to ambiguity.";
				throw new RpcException(RpcErrorCode.MethodNotFound, errorMessage);
			}

			throw new RpcException(RpcErrorCode.MethodNotFound, "No methods matched request.");
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
				Dictionary<string, IRpcParameterInfo> remainingParameters = parameters.ToDictionary(p => p.Name, p => p);
				foreach ((Memory<char> name, RpcParameterType sourceType) in requestSignature.ParametersAsDict)
				{
					bool found = false;
					for (int paramIndex = 0; paramIndex < parameters.Count; paramIndex++)
					{
						IRpcParameterInfo parameter = parameters[paramIndex];
						if (!RpcUtil.NamesMatch(parameter.Name.AsSpan(), name.Span))
						{
							continue;
						}
						RpcParameterType destinationType = this.rpcParameterConverter.GetRpcParameterType(parameter.RawType);
						if (!this.rpcParameterConverter.AreTypesCompatible(sourceType, destinationType))
						{
							continue;
						}
						remainingParameters.Remove(parameter.Name);
						found = true;
						break;
					}
					if (!found)
					{
						return false;
					}
					parameterCount++;
				}

				if (remainingParameters.Any(p => !p.Value.IsOptional))
				{
					return false;
				}
			}
			else
			{
				foreach (RpcParameterType sourceType in requestSignature.ParametersAsList)
				{
					if (parameters.Count <= parameterCount)
					{
						return false;
					}
					IRpcParameterInfo info = parameters[parameterCount];
					RpcParameterType destinationType = this.rpcParameterConverter.GetRpcParameterType(info.RawType);
					if (!this.rpcParameterConverter.AreTypesCompatible(sourceType, destinationType))
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
			return true;

		}

	}


	public class RequestCacheOptions
	{
		public long SizeLimit { get; set; } = 100;
		public TimeSpan? SlidingExpiration { get; set; } = null;
		public TimeSpan? AbsoluteExpiration { get; set; } = null;
	}

	internal class RequestMatcherCache
	{
		private RequestCacheOptions options { get; }
		private MemoryCache memoryCache { get; }

		public RequestMatcherCache(IOptions<RequestCacheOptions> options)
		{
			this.options = options.Value;
			this.memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions
			{
				SizeLimit = this.options.SizeLimit
			}));
		}

		public IRpcMethodInfo[] GetOrAdd(
			string path,
			RpcRequestSignature requestSignature,
			Func<IRpcMethodInfo[]> resolveFunc
		)
		{
			var cacheKey = new RequestCacheKey(path, requestSignature);

			if (this.memoryCache.TryGetValue(cacheKey, out IRpcMethodInfo[]? result))
			{
				return result!;
			}

			IRpcMethodInfo[] matchingMethods = resolveFunc();

			if (matchingMethods.Length != 1)
			{
				return matchingMethods; // Don't cache bad matches
			}

			// Cache with configurable options
			var cacheEntryOptions = new MemoryCacheEntryOptions()
				.SetSize(1);
			if (this.options.SlidingExpiration != null)
			{
				cacheEntryOptions = cacheEntryOptions.SetSlidingExpiration(this.options.SlidingExpiration.Value);
			}
			if (this.options.AbsoluteExpiration != null)
			{
				cacheEntryOptions = cacheEntryOptions.SetAbsoluteExpiration(this.options.AbsoluteExpiration.Value);
			}
			return this.memoryCache.Set(cacheKey, matchingMethods, cacheEntryOptions);
		}
	}

	internal readonly struct RequestCacheKey : IEquatable<RequestCacheKey>
	{
		private readonly string path;
		private readonly string signatureKey;

		public RequestCacheKey(string path, RpcRequestSignature signature)
		{
			this.path = path;
			this.signatureKey = signature.AsString();
		}

		public bool Equals(RequestCacheKey other) => this.path == other.path && this.signatureKey == other.signatureKey;

		public override bool Equals(object? obj) => obj is RequestCacheKey other && this.Equals(other);

		public override int GetHashCode() => HashCode.Combine(this.path, this.signatureKey);
	}


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

}