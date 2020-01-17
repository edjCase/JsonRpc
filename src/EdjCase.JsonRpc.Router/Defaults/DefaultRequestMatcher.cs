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
		private static ConcurrentDictionary<MethodInfo, CompiledMethodInfo> compiledMethodCache { get; } = new ConcurrentDictionary<MethodInfo, CompiledMethodInfo>();
		private static ConcurrentDictionary<string, CompiledMethodInfo[]> requestToMethodCache { get; } = new ConcurrentDictionary<string, CompiledMethodInfo[]>();

		private ILogger<DefaultRequestMatcher> logger { get; }
		private IOptions<RpcServerConfiguration> serverConfig { get; }
		public DefaultRequestMatcher(ILogger<DefaultRequestMatcher> logger,
		IOptions<RpcServerConfiguration> serverConfig)
		{
			this.logger = logger;
			this.serverConfig = serverConfig;
		}

		public RpcMethodInfo GetMatchingMethod(RpcRequest request, IReadOnlyList<MethodInfo> methods)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			this.logger.AttemptingToMatchMethod(request.Method);

			CompiledMethodInfo[] compiledMethods = ArrayPool<CompiledMethodInfo>.Shared.Rent(methods.Count);
			Span<RpcMethodInfo> matches;
			try
			{
				this.FillCompiledMethodInfos(methods, compiledMethods);
				matches = this.FilterAndBuildMethodInfoByRequest(compiledMethods.AsMemory(0, methods.Count), request);
			}
			finally
			{
				ArrayPool<CompiledMethodInfo>.Shared.Return(compiledMethods, clearArray: true);
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
				this.logger.MethodsInRoute(methods);
				errorMessage = "No methods matched request.";
			}
			throw new RpcException(RpcErrorCode.MethodNotFound, errorMessage);
		}

		private void FillCompiledMethodInfos(IReadOnlyList<MethodInfo> methods, CompiledMethodInfo[] compiledMethods)
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
						return new CompiledParameterInfo(parameterInfo.Name, type, parameterInfo.ParameterType, parameterInfo.IsOptional);
					}
				}
				compiledMethods[i] = info;
			}
		}

		private RpcMethodInfo[] FilterAndBuildMethodInfoByRequest(Memory<CompiledMethodInfo> methods, RpcRequest request)
		{
			using (RequestSignature requestSignature = RequestSignature.Create(request))
			{
				//If the request signature is found, it means we have the methods cached already
				//TODO make a cache that uses spans/char array and not strings
				//TODO does the entire method info need to be cached?
				CompiledMethodInfo[] filteredMethods = DefaultRequestMatcher.requestToMethodCache.GetOrAdd(requestSignature.AsString(), BuildCache);

				RpcMethodInfo[] potentialResults = ArrayPool<RpcMethodInfo>.Shared.Rent(filteredMethods.Length);
				try
				{
					int potentialResultsCount = 0;
					foreach (CompiledMethodInfo method in filteredMethods)
					{
						if (!this.TryParseParameters(method, request, out object[] paramList))
						{
							continue;
						}
						potentialResults[potentialResultsCount++] = new RpcMethodInfo(method.MethodInfo, paramList);
					}
					return potentialResults.AsSpan(0, potentialResultsCount).ToArray();
				}
				finally
				{
					ArrayPool<RpcMethodInfo>.Shared.Return(potentialResults);
				}

				CompiledMethodInfo[] BuildCache(string s)
				{
					return this.BuildMethodInfos(requestSignature, methods.Span);
				}
			}
		}


		private CompiledMethodInfo[] BuildMethodInfos(RequestSignature requestSignature, Span<CompiledMethodInfo> methods)
		{
			CompiledMethodInfo[] methodsWithSameName = ArrayPool<CompiledMethodInfo>.Shared.Rent(methods.Length);
			try
			{
				//Case insenstive check for hybrid approach. Will check for case sensitive if there is ambiguity
				int methodsWithSameNameCount = 0;
				for (int i = 0; i < methods.Length; i++)
				{
					CompiledMethodInfo compiledMethodInfo = methods[i];
					if (RpcUtil.NamesMatch(compiledMethodInfo.MethodInfo.Name.AsSpan(), requestSignature.GetMethodName()))
					{
						methodsWithSameName[methodsWithSameNameCount++] = compiledMethodInfo;
					}
				}
				if (methodsWithSameNameCount < 1)
				{
					return Array.Empty<CompiledMethodInfo>();
				}
				return this.FilterBySimilarParams(requestSignature, methodsWithSameName.AsSpan(0, methodsWithSameNameCount));
			}
			finally
			{
				ArrayPool<CompiledMethodInfo>.Shared.Return(methodsWithSameName, clearArray: false);
			}
		}

		private CompiledMethodInfo[] FilterBySimilarParams(RequestSignature requestSignature, Span<CompiledMethodInfo> methodsWithSameName)
		{
			CompiledMethodInfo[] potentialMatches = ArrayPool<CompiledMethodInfo>.Shared.Rent(methodsWithSameName.Length);

			try
			{
				int potentialMatchCount = 0;
				for (int i = 0; i < methodsWithSameName.Length; i++)
				{
					CompiledMethodInfo m = methodsWithSameName[i];

					bool isMatch = requestSignature.IsMatch(m);
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
				ArrayPool<CompiledMethodInfo>.Shared.Return(potentialMatches, clearArray: false);
			}
		}

		private CompiledMethodInfo[] FilterByExactParams(RequestSignature requestSignature, Span<CompiledMethodInfo> potentialMatches)
		{
			CompiledMethodInfo[] exactParamMatches = ArrayPool<CompiledMethodInfo>.Shared.Rent(potentialMatches.Length);
			try
			{
				int exactParamMatchCount = 0;
				//Try to remove ambiguity with 'perfect matching' (usually optional params and types)
				for (int i = 0; i < potentialMatches.Length; i++)
				{
					bool matched = true;
					CompiledMethodInfo info = potentialMatches[i];
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
				Span<CompiledMethodInfo> matches = exactParamMatchCount > 0 ? exactParamMatches.AsSpan(0, exactParamMatchCount) : potentialMatches;

				if (matches.Length <= 1)
				{
					return matches.ToArray();
				}
				return this.FilterMatchesByCaseSensitiveMethod(requestSignature, matches);
			}
			finally
			{
				ArrayPool<CompiledMethodInfo>.Shared.Return(exactParamMatches, clearArray: false);
			}
		}

		private CompiledMethodInfo[] FilterMatchesByCaseSensitiveMethod(RequestSignature requestSignature, Span<CompiledMethodInfo> matches)
		{
			//Try to remove ambiguity with case sensitive check
			CompiledMethodInfo[] caseSensitiveMatches = ArrayPool<CompiledMethodInfo>.Shared.Rent(matches.Length);
			try
			{
				int caseSensitiveCount = 0;
				for (int i = 0; i < matches.Length; i++)
				{
					CompiledMethodInfo m = matches[i];
					Span<char> requestMethodName = requestSignature.GetMethodName();
					if (m.MethodInfo.Name.Length == requestMethodName.Length)
					{
						bool namesMatch = true;
						for (int j = 0; j < m.MethodInfo.Name.Length; j++)
						{
							if (m.MethodInfo.Name[i] != requestMethodName[i])
							{
								namesMatch = false;
								break;
							}
						}
						if (namesMatch)
						{
							caseSensitiveMatches[caseSensitiveCount++] = m;
						}
					}
				}
				return caseSensitiveMatches.AsSpan(0, caseSensitiveCount).ToArray();
			}
			finally
			{
				ArrayPool<CompiledMethodInfo>.Shared.Return(caseSensitiveMatches, clearArray: false);
			}
		}



		/// <summary>
		/// Detects if list of parameters matches the method signature
		/// </summary>
		/// <param name="parameterList">Array of parameters for the method</param>
		/// <returns>True if the method signature matches the parameterList, otherwise False</returns>
		private bool TryParseParameters(CompiledMethodInfo method, RpcRequest rpcRequest, out object[] parsedParameters)
		{
			//TODO this has some overlap with RequestSignature.IsMatch
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
						parsedParameters = null;
						return false;
					}
				}
				else
				{
					parameters = rpcRequest.Parameters.AsList;
				}
			}
			if (parameters.Count() > method.Parameters.Length)
			{
				parsedParameters = null;
				return false;
			}


			object[] paramCache = ArrayPool<object>.Shared.Rent(method.Parameters.Length);
			try
			{
				if (parameters.Any())
				{
					for (int i = 0; i < parameters.Count(); i++)
					{
						CompiledParameterInfo parameterInfo = method.Parameters[i];
						IRpcParameter parameter = parameters[i];
						if (!parameter.TryGetValue(parameterInfo.RawType, out object value))
						{
							parsedParameters = null;
							return false;
						}
						paramCache[i] = value;
					}
					//Only make an array if needed
					var deserializedParameters = new object[method.Parameters.Length];
					paramCache
						.AsSpan(0, method.Parameters.Length)
						.CopyTo(deserializedParameters);
					parsedParameters = deserializedParameters;
				}
				else
				{
					parsedParameters = new object[method.Parameters.Length];
				}
				return true;
			}
			finally
			{
				ArrayPool<object>.Shared.Return(paramCache, clearArray: false);
			}
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
						//TODO do we care about the case where 2+ parameters have very similar names and types?
						parameterList[i] = requestParameter.Value;
						break;
					}
				}
				if(parameterList[i] == null)
				{
					//Doesn't match the names of any
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

		private struct RequestSignature : IDisposable
		{
			private const char delimiter = ' ';
			private const char numberType = 'n';
			private const char stringType = 's';
			private const char booleanType = 'b';
			private const char objectType = 'o';
			private const char nullType = '-';

			private char[] Values { get; set; }
			private int MethodEndIndex { get; set; }
			private bool IsDictionary { get; set; }
			private int? ParamStartIndex { get; set; }
			private int EndIndex { get; set; }


			public Span<char> GetMethodName()
			{
				return this.Values.AsSpan(0, this.MethodEndIndex + 1);
			}

			public void Dispose()
			{
				ArrayPool<char>.Shared.Return(this.Values);
			}

			internal string AsString()
			{
				return new string(this.Values, 0, this.EndIndex + 1);
			}


			public static RequestSignature Create(RpcRequest request)
			{
				//TODO size
				int initialParamSize = 200;
				int arraySize = request.Method.Length;
				if (request.Parameters != null)
				{
					arraySize += 3 + initialParamSize;
				}

				char[] requestSignatureArray = ArrayPool<char>.Shared.Rent(arraySize);
				int signatureLength = 0;
				const int incrementSize = 30;
				for (int a = 0; a < request.Method.Length; a++)
				{
					requestSignatureArray[signatureLength++] = request.Method[a];
				}
				int methodEndIndex = signatureLength - 1;
				int? parameterStartIndex = null;
				bool isDictionary = false;
				if (request.Parameters != null)
				{
					requestSignatureArray[signatureLength++] = delimiter;
					requestSignatureArray[signatureLength++] = request.Parameters.IsDictionary ? 'd' : 'a';
					if (request.Parameters.Any())
					{
						parameterStartIndex = signatureLength + 1;
						if (request.Parameters.IsDictionary)
						{
							isDictionary = true;
							foreach (KeyValuePair<string, IRpcParameter> parameter in request.Parameters.AsDictionary)
							{
								int greatestIndex = signatureLength + parameter.Key.Length + 1;
								if (greatestIndex >= requestSignatureArray.Length)
								{
									ArrayPool<char>.Shared.Return(requestSignatureArray);
									requestSignatureArray = ArrayPool<char>.Shared.Rent(requestSignatureArray.Length + incrementSize);
								}
								requestSignatureArray[signatureLength++] = delimiter;
								for (int i = 0; i < parameter.Key.Length; i++)
								{
									requestSignatureArray[signatureLength++] = parameter.Key[i];
								}
								requestSignatureArray[signatureLength++] = delimiter;
								requestSignatureArray[signatureLength++] = RequestSignature.GetCharFromType(parameter.Value.Type);
							}
						}
						else
						{
							requestSignatureArray[signatureLength++] = delimiter;
							List<IRpcParameter> list = request.Parameters.AsList;
							for (int i = 0; i < list.Count; i++)
							{
								char c = RequestSignature.GetCharFromType(list[i].Type);
								requestSignatureArray[signatureLength++] = c;
							}
						}
					}
				}
				return new RequestSignature
				{
					Values = requestSignatureArray,
					MethodEndIndex = methodEndIndex,
					IsDictionary = isDictionary,
					ParamStartIndex = parameterStartIndex,
					EndIndex = signatureLength - 1
				};
			}

			internal bool IsMatch(CompiledMethodInfo m)
			{
				if (this.ParamStartIndex == null)
				{
					return m.Parameters == null || !m.Parameters.Any(p => !p.IsOptional);
				}
				if (this.IsDictionary)
				{
					int currentParamCount = 0;
					int i = this.ParamStartIndex.Value;
					int currentKeyLength = 0;
					bool hasParams = this.EndIndex - i > 0;
					if (hasParams != m.Parameters.Any())
					{
						return false;
					}
					for (; i < this.EndIndex; i++)
					{
						if (this.Values[i] != RequestSignature.delimiter)
						{
							if (m.Parameters[currentParamCount].Name.Length <= currentKeyLength
								|| this.Values[i] != m.Parameters[currentParamCount].Name[currentKeyLength])
							{
								//Name doesnt match
								return false;
							}
							//Key char matched
							currentKeyLength++;
							continue;
						}
						if(m.Parameters.Length <= currentParamCount)
						{
							return false;
						}
						//Delimiter/end of the key
						if (m.Parameters[currentParamCount].Name.Length != currentKeyLength)
						{
							//Key length isnt the same
							return false;
						}
						//Key matches, now check the value type
						i++;
						if (this.EndIndex < i)
						{
							return false;
						}
						RpcParameterType requestType = RequestSignature.GetTypeFromChar(this.Values[i]);
						if (!RpcParameterUtil.TypesCompatible(requestType, m.Parameters[currentParamCount].Type))
						{
							return false;
						}
						currentParamCount++;
						//reset key length, moving to next
						currentKeyLength = 0;
						//Skip next delimiter
						i++;

					}
					if (m.Parameters.Length != currentParamCount)
					{
						//Param count dont match
						return false;
					}
				}
				else
				{
					int methodLength = this.EndIndex - this.ParamStartIndex.Value;
					if (m.Parameters.Length < methodLength)
					{
						//Method has fewer params than the request 
						return false;
					}
					int paramIndex = 0;
					for (int i = this.ParamStartIndex.Value; i <= this.EndIndex; i++)
					{
						char paramType = this.Values[i];
						RpcParameterType type = RequestSignature.GetTypeFromChar(paramType);
						if (!RpcParameterUtil.TypesCompatible(type, m.Parameters[paramIndex++].Type))
						{
							return false;
						}
					}
					for (int i = methodLength; i >= m.Parameters.Length; i++)
					{
						//Only if the last parameters in the method are optional does the request match
						//Will be skipped if they are equal length
						if (!m.Parameters[i].IsOptional)
						{
							return false;
						}
					}
				}
				return true;
			}

			private static char GetCharFromType(RpcParameterType type)
			{
				switch (type)
				{
					case RpcParameterType.String:
						return RequestSignature.stringType;
					case RpcParameterType.Boolean:
						return RequestSignature.booleanType;
					case RpcParameterType.Object:
						return RequestSignature.objectType;
					case RpcParameterType.Number:
						return RequestSignature.numberType;
					case RpcParameterType.Null:
						return RequestSignature.nullType;
					default:
						throw new InvalidOperationException($"Unimplemented parameter type '{type}'");

				}
			}

			private static RpcParameterType GetTypeFromChar(char type)
			{
				switch (type)
				{
					case RequestSignature.stringType:
						return RpcParameterType.String;
					case RequestSignature.booleanType:
						return RpcParameterType.Boolean;
					case RequestSignature.objectType:
						return RpcParameterType.Object;
					case RequestSignature.numberType:
						return RpcParameterType.Number;
					case RequestSignature.nullType:
						return RpcParameterType.Null;
					default:
						throw new InvalidOperationException($"Unimplemented parameter type '{type}'");

				}
			}
		}
	}
}