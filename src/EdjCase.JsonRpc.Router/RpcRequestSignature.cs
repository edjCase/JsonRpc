using EdjCase.JsonRpc.Common;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Router
{
	internal class RpcRequestSignature : IDisposable
	{
		private const char arrayType = 'a';
		private const char dictType = 'd';
		private const char delimiter = ' ';
		private const char numberType = 'n';
		private const char stringType = 's';
		private const char booleanType = 'b';
		private const char objectType = 'o';
		private const char nullType = '-';

		private readonly char[] values;
		private readonly int methodEndIndex;
		private readonly int? paramStartIndex;
		private readonly int endIndex;
		public bool IsDictionary { get; }

		private RpcRequestSignature(char[] values, int methodEndIndex, int? paramStartIndex, int endIndex, bool isDictionary)
		{
			this.values = values;
			this.methodEndIndex = methodEndIndex;
			this.paramStartIndex = paramStartIndex;
			this.endIndex = endIndex;
			this.IsDictionary = isDictionary;
		}

		public Memory<char> GetMethodName() => this.values.AsMemory(0, this.methodEndIndex + 1);
		public bool HasParameters => this.paramStartIndex != null;

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
					int i = this.paramStartIndex!.Value;
					int currentKeyLength = 0;
					for (; i <= this.endIndex; i++)
					{
						if (this.values[i] != RpcRequestSignature.delimiter)
						{
							//Key finished
							currentKeyLength++;
							continue;
						}
						Memory<char> key = this.values.AsMemory(i - currentKeyLength, currentKeyLength);
						RpcParameterType type = RpcRequestSignature.GetTypeFromChar(this.values[i + 1]);
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
					for (int i = this.paramStartIndex!.Value; i <= this.endIndex; i++)
					{
						yield return RpcRequestSignature.GetTypeFromChar(this.values[i]);
					}
				}
			}
		}

		public void Dispose()
		{
			ArrayPool<char>.Shared.Return(this.values);
		}

		public override int GetHashCode()
		{
			return new string(this.values).GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
			{
				return false;
			}
			if (!(obj is RpcRequestSignature other))
			{
				return false;
			}
			if (this.values.Length != other.values.Length)
			{
				return false;
			}
			for (int i = 0; i < this.values.Length; i++)
			{
				if (this.values[i] != other.values[i])
				{
					return false;
				}
			}
			return true;
		}

		internal string AsString()
		{
			return new string(this.values, 0, this.endIndex + 1);
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
						if (signatureLength + 1 == parameterStartIndex)
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
						if (parameterStartIndex == signatureLength)
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
			return type switch
			{
				RpcParameterType.String => RpcRequestSignature.stringType,
				RpcParameterType.Boolean => RpcRequestSignature.booleanType,
				RpcParameterType.Object => RpcRequestSignature.objectType,
				RpcParameterType.Number => RpcRequestSignature.numberType,
				RpcParameterType.Null => RpcRequestSignature.nullType,
				RpcParameterType.Array => RpcRequestSignature.arrayType,
				_ => throw new InvalidOperationException($"Unimplemented parameter type '{type}'"),
			};
		}

		private static RpcParameterType GetTypeFromChar(char type)
		{
			return type switch
			{
				RpcRequestSignature.stringType => RpcParameterType.String,
				RpcRequestSignature.booleanType => RpcParameterType.Boolean,
				RpcRequestSignature.objectType => RpcParameterType.Object,
				RpcRequestSignature.numberType => RpcParameterType.Number,
				RpcRequestSignature.nullType => RpcParameterType.Null,
				RpcRequestSignature.arrayType => RpcParameterType.Array,
				_ => throw new InvalidOperationException($"Unimplemented parameter type '{type}'"),
			};
		}
	}



}
