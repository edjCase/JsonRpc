using EdjCase.JsonRpc.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Router
{
	public class RpcRequestSignature : IDisposable
	{
		private const char arrayType = 'a';
		private const char dictType = 'd';
		private const char delimiter = ' ';
		private const char numberType = 'n';
		private const char stringType = 's';
		private const char booleanType = 'b';
		private const char objectType = 'o';
		private const char nullType = '-';

		private char[] Values { get; }
		private int MethodEndIndex { get; }
		private int? ParamStartIndex { get; }
		private int EndIndex { get; }
		public bool IsDictionary { get; }

		private RpcRequestSignature(char[] values, int methodEndIndex, int? paramStartIndex, int endIndex, bool isDictionary)
		{
			this.Values = values;
			this.MethodEndIndex = methodEndIndex;
			this.ParamStartIndex = paramStartIndex;
			this.EndIndex = endIndex;
			this.IsDictionary = isDictionary;
		}




		public Memory<char> GetMethodName() => this.Values.AsMemory(0, this.MethodEndIndex + 1);
		public bool HasParameters => this.ParamStartIndex != null;

		public IEnumerable<(Memory<char>, RpcParameterType)> ParametersAsDict
		{
			get
			{
				if (!this.IsDictionary)
				{
					throw new InvalidOperationException("Cannot get parameters as a dictionary");
				}
				if (this.HasParameters)
				{
					int i = this.ParamStartIndex!.Value;
					int currentKeyLength = 0;
					for (; i <= this.EndIndex; i++)
					{
						if (this.Values[i] != RpcRequestSignature.delimiter)
						{
							//Key finished
							currentKeyLength++;
							continue;
						}
						Memory<char> key = this.Values.AsMemory(i - currentKeyLength, currentKeyLength);
						RpcParameterType type = RpcRequestSignature.GetTypeFromChar(this.Values[i + 1]);
						yield return (key, type);
						//reset key length, moving to next
						currentKeyLength = 0;
						//Skip next delimiter
						i += 2;
					}
				}
			}
		}

		public IEnumerable<RpcParameterType> ParametersAsList
		{
			get
			{
				if (this.IsDictionary)
				{
					throw new InvalidOperationException("Cannot get parameters as a list");
				}
				if (this.HasParameters)
				{
					for (int i = this.ParamStartIndex!.Value; i <= this.EndIndex; i++)
					{
						yield return RpcRequestSignature.GetTypeFromChar(this.Values[i]);
					}
				}
			}
		}

		public void Dispose()
		{
			ArrayPool<char>.Shared.Return(this.Values);
		}

		internal string AsString()
		{
			return new string(this.Values, 0, this.EndIndex + 1);
		}


		public static RpcRequestSignature Create(RpcRequest request)
		{
			if (request.Parameters == null || !request.Parameters.IsDictionary)
			{
				return RpcRequestSignature.Create(request.Method, request.Parameters?.AsArray.Select(p => p.Type));
			}
			return RpcRequestSignature.Create(request.Method, request.Parameters.AsDictionary.Select(p => new KeyValuePair<string, RpcParameterType>(p.Key, p.Value.Type)));
		}

		public static RpcRequestSignature Create(string methodName, IEnumerable<RpcParameterType>? parameters = null)
		{
			return RpcRequestSignature.CreateInternal(methodName, parameters);
		}

		public static RpcRequestSignature Create(string methodName, IEnumerable<KeyValuePair<string, RpcParameterType>>? parameters)
		{
			return RpcRequestSignature.CreateInternal(methodName, parameters);
		}

		private static RpcRequestSignature CreateInternal(string methodName, object? parameters)
		{
			//TODO size
			int initialParamSize = 200;
			int arraySize = methodName.Length;
			if (parameters != null)
			{
				arraySize += 3 + initialParamSize;
			}

			char[] requestSignatureArray = ArrayPool<char>.Shared.Rent(arraySize);
			int signatureLength = 0;
			const int incrementSize = 30;
			for (int a = 0; a < methodName.Length; a++)
			{
				requestSignatureArray[signatureLength++] = methodName[a];
			}
			int methodEndIndex = signatureLength - 1;
			int? parameterStartIndex = null;
			bool isDictionary = false;
			if (parameters != null)
			{
				requestSignatureArray[signatureLength++] = delimiter;
				switch (parameters)
				{
					case IEnumerable<KeyValuePair<string, RpcParameterType>> dictParam:
						requestSignatureArray[signatureLength++] = RpcRequestSignature.dictType;
						isDictionary = true;
						parameterStartIndex = signatureLength + 1;
						foreach (KeyValuePair<string, RpcParameterType> parameter in dictParam)
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
							requestSignatureArray[signatureLength++] = RpcRequestSignature.GetCharFromType(parameter.Value);
						}
						if(signatureLength + 1 == parameterStartIndex)
						{
							//There were no parameters
							parameterStartIndex = null;
						}
						break;
					case IEnumerable<RpcParameterType> listParam:
						requestSignatureArray[signatureLength++] = RpcRequestSignature.arrayType;
							requestSignatureArray[signatureLength++] = delimiter;
							parameterStartIndex = signatureLength;
							foreach (RpcParameterType type in listParam)
							{
								char c = RpcRequestSignature.GetCharFromType(type);
								requestSignatureArray[signatureLength++] = c;
							}
							if(parameterStartIndex == signatureLength)
							{
								//No parameters, remove the delimeter
								signatureLength--;
								parameterStartIndex = null;
							}
						break;
					case null:
						requestSignatureArray[signatureLength++] = RpcRequestSignature.arrayType;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(parameters));
				}
			}
			return new RpcRequestSignature(requestSignatureArray, methodEndIndex, parameterStartIndex, signatureLength - 1, isDictionary);
		}


		private static char GetCharFromType(RpcParameterType type)
		{
			switch (type)
			{
				case RpcParameterType.String:
					return RpcRequestSignature.stringType;
				case RpcParameterType.Boolean:
					return RpcRequestSignature.booleanType;
				case RpcParameterType.Object:
					return RpcRequestSignature.objectType;
				case RpcParameterType.Number:
					return RpcRequestSignature.numberType;
				case RpcParameterType.Null:
					return RpcRequestSignature.nullType;
				default:
					throw new InvalidOperationException($"Unimplemented parameter type '{type}'");

			}
		}

		private static RpcParameterType GetTypeFromChar(char type)
		{
			switch (type)
			{
				case RpcRequestSignature.stringType:
					return RpcParameterType.String;
				case RpcRequestSignature.booleanType:
					return RpcParameterType.Boolean;
				case RpcRequestSignature.objectType:
					return RpcParameterType.Object;
				case RpcRequestSignature.numberType:
					return RpcParameterType.Number;
				case RpcRequestSignature.nullType:
					return RpcParameterType.Null;
				default:
					throw new InvalidOperationException($"Unimplemented parameter type '{type}'");

			}
		}
	}



}
