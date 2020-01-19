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
		Object
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

		public static bool TypesCompatible(RpcParameterType requestType, RpcParameterType methodType)
		{
			switch (requestType)
			{
				case RpcParameterType.Boolean:
					return methodType == RpcParameterType.Boolean || methodType == RpcParameterType.Object;
				case RpcParameterType.Number:
					//Almost anything can be converted from a number
					return true;
				case RpcParameterType.Object:
					return methodType == RpcParameterType.Object;
				case RpcParameterType.Null:
				case RpcParameterType.String:
					return methodType == RpcParameterType.String || methodType == RpcParameterType.Object;
				default:
					throw new ArgumentOutOfRangeException(nameof(requestType));
			}
		}
	}

	internal class RawRpcParameter : IRpcParameter
	{
		public RpcParameterType Type { get; }
		public object? Value { get; }
		public RawRpcParameter(RpcParameterType type, object? value)
		{
			this.Type = type;
			this.Value = value;
		}

		public bool TryGetValue(Type type, out object? value)
		{
			if (this.Type == RpcParameterType.Null)
			{
				value = null;
				return type.IsNullableType();
			}
			if (this.Value == null)
			{
				value = null;
				return false;
			}
			Type parameterType = this.Value.GetType();

			if (parameterType == type || type.IsAssignableFrom(parameterType))
			{
				value = this.Value;
				return true;
			}
			TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
			if (typeConverter != null)
			{
				if (typeConverter.CanConvertFrom(parameterType))
				{
					value = typeConverter.ConvertFrom(this.Value);
					return true;
				}
			}
			TypeConverter parameterTypeConverter = TypeDescriptor.GetConverter(parameterType);
			if (parameterTypeConverter != null)
			{
				if (parameterTypeConverter.CanConvertTo(type))
				{
					value = parameterTypeConverter.ConvertTo(this.Value, type);
					return true;
				}
			}

			value = default;
			return false;
		}
	}

	internal class JsonBytesRpcParameter : IRpcParameter
	{
		public RpcParameterType Type { get; }
		private Memory<byte> bytes { get; }
		private JsonSerializerOptions? serializerOptions { get; }

		public JsonBytesRpcParameter(RpcParameterType type, Memory<byte> bytes, JsonSerializerOptions? serializerOptions = null)
		{
			this.Type = type;
			this.bytes = bytes;
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
				value = JsonSerializer.Deserialize(this.bytes.Span, type, this.serializerOptions);
				return true;
			}
			catch (Exception)
			{
				value = default;
				return false;
			}
		}
	}
}
