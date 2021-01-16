using EdjCase.JsonRpc.Router.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router
{
	public interface IRpcParameter
	{
		RpcParameterType Type { get; }
		bool TryGetValue(Type type, out object? value);
	}

	public enum RpcParameterType
	{
		Null,
		Boolean,
		Number,
		String,
		Object,
		Array
	}

	public static class RpcParameterUtil
	{
		public static bool TryGetValue<T>(this IRpcParameter parameter, out T value)
		{
			bool parsed = parameter.TryGetValue(typeof(T), out object? v);
			if (parsed)
			{
				value = (T)v!;
				return true;
			}
			value = default!;
			return false;
		}

		public static bool TypesCompatible(RpcParameterType actualType, RpcParameterType requestedType)
		{
			switch (requestedType)
			{
				case RpcParameterType.Boolean:
					return actualType == RpcParameterType.Boolean || actualType == RpcParameterType.Object;
				case RpcParameterType.Number:
					//Almost anything can be converted from a number
					return actualType != RpcParameterType.Array;
				case RpcParameterType.Object:
					return actualType == RpcParameterType.Object;
				case RpcParameterType.Null:
				case RpcParameterType.String:
					return actualType == RpcParameterType.String || actualType == RpcParameterType.Object;
				case RpcParameterType.Array:
					return actualType == RpcParameterType.Object || actualType == RpcParameterType.Array;
				default:
					throw new ArgumentOutOfRangeException(nameof(requestedType));
			}
		}

		internal static RpcParameterType GetRpcType(Type parameterType)
		{
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
				return RpcParameterType.Number;
			}
			if (parameterType == typeof(string))
			{
				return RpcParameterType.String;
			}
			if (parameterType == typeof(bool))
			{
				return RpcParameterType.Boolean;
			}
			return RpcParameterType.Object;
		}
	}

	internal class JsonBytesRpcParameter : IRpcParameter
	{
		public RpcParameterType Type { get; }
		private byte[] bytes { get; }
		private JsonSerializerOptions? serializerOptions { get; }

		public JsonBytesRpcParameter(RpcParameterType type, Memory<byte> bytes, JsonSerializerOptions? serializerOptions = null)
		{
			this.Type = type;
			this.bytes = bytes.ToArray();
			this.serializerOptions = serializerOptions;
		}

		public bool TryGetValue(Type type, out object? value)
		{
			if (this.Type == RpcParameterType.Null)
			{
				value = null;
				return type.IsNullableType();
			}
			
			try
			{
				value = JsonSerializer.Deserialize(this.bytes, type, this.serializerOptions);
				return true;
			}
			catch (Exception)
			{
				value = default;
				return false;
			}
		}

		public static JsonBytesRpcParameter FromRaw(object? value, JsonSerializerOptions? serializerOptions = null)
		{
			byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
			RpcParameterType type = value != null ? RpcParameterUtil.GetRpcType(value.GetType()) : RpcParameterType.Null;
			return new JsonBytesRpcParameter(type, jsonBytes, serializerOptions);
		}
	}
}
