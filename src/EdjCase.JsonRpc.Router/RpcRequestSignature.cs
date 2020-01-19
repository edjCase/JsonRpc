using EdjCase.JsonRpc.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Router
{
	public struct RpcRequestSignature : IDisposable
	{
		private const char delimiter = ' ';
		private const char numberType = 'n';
		private const char stringType = 's';
		private const char booleanType = 'b';
		private const char objectType = 'o';
		private const char nullType = '-';

		private char[] Values { get; set; }
		private int MethodEndIndex { get; set; }
		private int? ParamStartIndex { get; set; }
		private int EndIndex { get; set; }


		public Memory<char> GetMethodName() => this.Values.AsMemory(0, this.MethodEndIndex + 1);
		public bool HasParameters => this.ParamStartIndex != null;
		public bool IsDictionary { get; set; }

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
					for (; i < this.EndIndex; i++)
					{
						if (this.Values[i] != RpcRequestSignature.delimiter)
						{
							//Key finished
							currentKeyLength++;
							continue;
						}
						Memory<char> key = this.Values.AsMemory(i, currentKeyLength);
						RpcParameterType type = RpcRequestSignature.GetTypeFromChar(this.Values[i + currentKeyLength]);
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
					for (int i = this.ParamStartIndex!.Value; i < this.EndIndex; i++)
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
							requestSignatureArray[signatureLength++] = RpcRequestSignature.GetCharFromType(parameter.Value.Type);
						}
					}
					else
					{
						requestSignatureArray[signatureLength++] = delimiter;
						IRpcParameter[] list = request.Parameters.AsArray;
						for (int i = 0; i < list.Length; i++)
						{
							char c = RpcRequestSignature.GetCharFromType(list[i].Type);
							requestSignatureArray[signatureLength++] = c;
						}
					}
				}
			}
			return new RpcRequestSignature
			{
				Values = requestSignatureArray,
				MethodEndIndex = methodEndIndex,
				IsDictionary = isDictionary,
				ParamStartIndex = parameterStartIndex,
				EndIndex = signatureLength - 1
			};
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
