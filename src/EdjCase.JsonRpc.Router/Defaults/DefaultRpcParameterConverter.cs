using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router.Defaults
{
	internal class DefaultRpcParameterConverter : IRpcParameterConverter
	{
		private readonly IOptions<RpcServerConfiguration> options;
		private readonly ILogger<DefaultRpcParameterConverter> logger;

		public DefaultRpcParameterConverter(
			IOptions<RpcServerConfiguration> options,
			ILogger<DefaultRpcParameterConverter> logger)
		{
			this.options = options;
			this.logger = logger;
		}

		public bool TryConvertValue(
			RpcParameter sourceValue,
			RpcParameterType destinationType,
			Type destinationRawType,
			out object? destinationValue,
			out Exception? exception)
		{
			if(sourceValue.Type == RpcParameterType.Null
				&& !sourceValue.GetIfNullIsSpecified())
			{
				// Any unspecified type is 'missing'
				destinationValue = Type.Missing;
				exception = null;
				return true;
			}
			TryConvertFunc? func = this.TryGetConveterFunc(sourceValue.Type, destinationType);
			if (func == null)
			{
				destinationValue = false;
				exception = null;
				return false;
			}
			var context = new Context(sourceValue, destinationRawType, this.logger, this.options.Value.JsonSerializerSettings);
			return func(context, out destinationValue, out exception);
		}

		public bool AreTypesCompatible(RpcParameterType sourceType, RpcParameterType destinationType)
		{
			return this.TryGetConveterFunc(sourceType, destinationType) != null;
		}

		public RpcParameterType GetRpcParameterType(Type type)
		{
			//Convert Nullable<T> to T
			type = Nullable.GetUnderlyingType(type) ?? type;

			if (type == typeof(short)
				   || type == typeof(ushort)
				   || type == typeof(int)
				   || type == typeof(uint)
				   || type == typeof(long)
				   || type == typeof(ulong)
				   || type == typeof(float)
				   || type == typeof(double)
				   || type == typeof(decimal))
			{
				return RpcParameterType.Number;
			}
			if (type == typeof(string))
			{
				return RpcParameterType.String;
			}
			if (type == typeof(bool))
			{
				return RpcParameterType.Boolean;
			}
			if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
			{
				return RpcParameterType.Array;
			}
			return RpcParameterType.Object;

		}

		private class Context
		{
			public RpcParameter SourceValue { get; }
			public Type DestinationType { get; }
			public ILogger<DefaultRpcParameterConverter> Logger { get; }
			public JsonSerializerOptions? SerializerOptions { get; }
			public Context(
				RpcParameter sourceValue,
				Type destinationType,
				ILogger<DefaultRpcParameterConverter> logger,
				JsonSerializerOptions? serializerOptions)
			{
				this.SourceValue = sourceValue ?? throw new ArgumentNullException(nameof(sourceValue));
				this.DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
				this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
				this.SerializerOptions = serializerOptions;
			}

		}

		private delegate bool TryConvertFunc(Context context, out object? destinationValue,  out Exception? exception);

		private static readonly Dictionary<(RpcParameterType Source, RpcParameterType Destination), TryConvertFunc> converterFuncs = new Dictionary<(RpcParameterType Source, RpcParameterType Destination), TryConvertFunc>
		{
			[(RpcParameterType.Array, RpcParameterType.Array)] = TryGetArrayFromArray,
			[(RpcParameterType.Array, RpcParameterType.Object)] = TryGetArrayFromArray,
			[(RpcParameterType.Boolean, RpcParameterType.Boolean)] = TryGetBooleanFromBoolean,
			[(RpcParameterType.Boolean, RpcParameterType.Number)] = TryGetNumberFromBoolean,
			[(RpcParameterType.Boolean, RpcParameterType.String)] = TryGetStringFromBoolean,
			[(RpcParameterType.Number, RpcParameterType.Number)] = TryGetNumberFromNumber,
			[(RpcParameterType.Number, RpcParameterType.String)] = TryGetStringFromNumber,
			[(RpcParameterType.Object, RpcParameterType.Object)] = TryGetObjectFromObject,
			[(RpcParameterType.String, RpcParameterType.Boolean)] = TryGetBooleanFromString,
			[(RpcParameterType.String, RpcParameterType.Number)] = TryGetNumberFromString,
			[(RpcParameterType.String, RpcParameterType.String)] = TryGetStringFromString,
			//Useful for string values like Guids
			[(RpcParameterType.String, RpcParameterType.Object)] = TryGetObjectFromString,
		};

		private TryConvertFunc? TryGetConveterFunc(RpcParameterType sourceType, RpcParameterType destinationType)
		{
			if (sourceType == RpcParameterType.Null)
			{
				return TryGetNullFromAny;
			}
			return converterFuncs.GetValueOrDefault((sourceType, destinationType));
		}


		private static bool TryGetObjectFromString(Context context, out object? destinationValue,  out Exception? exception)
		{
			string json = context.SourceValue.GetStringValue();
			//Convert to list so it can be deserialized as proper json
			json = $"[\"{json}\"]";
			Type listDestinationType = typeof(List<>);
			listDestinationType = listDestinationType.MakeGenericType(context.DestinationType);
			var newContext = new Context(context.SourceValue, listDestinationType, context.Logger, context.SerializerOptions);

			bool canParse = TryDeserializeJson(json, newContext, out destinationValue, out exception);
			if (canParse)
			{
				destinationValue = ((IList)destinationValue!)[0];
			}
			return canParse;
		}

		private static bool TryGetObjectFromObject(Context context, out object? destinationValue,  out Exception? exception)
		{
			Dictionary<string, RpcParameter> obj = context.SourceValue.GetObjectValue();
			string json = JsonStringGeneratorUtil.FromObject(obj, context.SerializerOptions);
			return TryDeserializeJson(json, context, out destinationValue, out exception);
		}

		private static bool TryGetArrayFromArray(Context context, out object? destinationValue, out Exception? exception)
		{
			RpcParameter[] array = context.SourceValue.GetArrayValue();
			string json = JsonStringGeneratorUtil.FromArray(array, context.SerializerOptions);
			return TryDeserializeJson(json, context, out destinationValue, out exception);
		}

		private static bool TryDeserializeJson(string json, Context context, out object? destinationValue, out Exception? exception)
		{
			try
			{				
				destinationValue = JsonSerializer.Deserialize(json, context.DestinationType, context.SerializerOptions);
				exception = null;
				return true;
			}
			catch (Exception ex)
			{
				context.Logger.LogWarning(ex, $"Failed to convert parameter value '{json}' to type '{context.DestinationType.Name}' ");
				destinationValue = null;
				exception = ex;
				return false;
			}
		}

		private static bool TryGetBooleanFromString(Context context, out object? destinationValue,  out Exception? exception)
		{
			string value = context.SourceValue.GetStringValue();
			bool canParse = bool.TryParse(value, out bool boolValue);
			destinationValue = boolValue;
			exception = null;
			return canParse;
		}

		private static bool TryGetStringFromString(Context context, out object? destinationValue,  out Exception? exception)
		{
			string value = context.SourceValue.GetStringValue();
			destinationValue = value;
			exception = null;
			return true;
		}

		private static bool TryGetStringFromNumber(Context context, out object? destinationValue,  out Exception? exception)
		{
			RpcNumber value = context.SourceValue.GetNumberValue();
			destinationValue = value.ToString();
			exception = null;
			return true;
		}

		private static bool TryGetNumberFromBoolean(Context context, out object? destinationValue,  out Exception? exception)
		{
			bool value = context.SourceValue.GetBooleanValue();
			destinationValue = value ? 1 : 0;
			exception = null;
			return true;
		}

		private static bool TryGetBooleanFromBoolean(Context context, out object? destinationValue,  out Exception? exception)
		{
			bool value = context.SourceValue.GetBooleanValue();
			destinationValue = value;
			exception = null;
			return true;
		}

		private static bool TryGetNullFromAny(Context context, out object? destinationValue,  out Exception? exception)
		{
			destinationValue = null;
			bool canBeNull = !context.DestinationType.IsValueType
				|| Nullable.GetUnderlyingType(context.DestinationType) != null;
			exception = null;
			return canBeNull;
		}

		private static bool TryGetStringFromBoolean(Context context, out object? destinationValue,  out Exception? exception)
		{
			bool value = context.SourceValue.GetBooleanValue();
			destinationValue = value ? "true" : "false";
			exception = null;
			return true;
		}

		private static bool TryGetNumberFromNumber(Context context, out object? destinationValue,  out Exception? exception)
		{
			RpcNumber number = context.SourceValue.GetNumberValue();
			exception = null;
			return TryGetNumberInternal(number, context.DestinationType, out destinationValue);
		}

		private static bool TryGetNumberFromString(Context context, out object? destinationValue,  out Exception? exception)
		{
			string numberString = context.SourceValue.GetStringValue();
			var number = new RpcNumber(numberString);
			exception = null;
			return TryGetNumberInternal(number, context.DestinationType, out destinationValue);
		}

		private static bool TryGetNumberInternal(RpcNumber number, Type destinationRawType, out object? destinationValue)
		{
			bool canParse;
			Type nonNullableType = Nullable.GetUnderlyingType(destinationRawType) ?? destinationRawType;
			if (nonNullableType == typeof(int))
			{
				canParse = number.TryGetInteger(out int v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(long))
			{
				canParse = number.TryGetLong(out long v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(float))
			{
				canParse = number.TryGetFloat(out float v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(double))
			{
				canParse = number.TryGetDouble(out double v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(decimal))
			{
				canParse = number.TryGetDecimal(out decimal v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(short))
			{
				canParse = number.TryGetShort(out short v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(ushort))
			{
				canParse = number.TryGetUnsignedShort(out ushort v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(uint))
			{
				canParse = number.TryGetUnsignedInteger(out uint v);
				destinationValue = v;
			}
			else if (nonNullableType == typeof(ulong))
			{
				canParse = number.TryGetUnsingedLong(out ulong v);
				destinationValue = v;
			}
			else
			{
				throw new NotImplementedException($"Number type '{nonNullableType.Name}'");
			}
			return canParse;
		}
	}
}
