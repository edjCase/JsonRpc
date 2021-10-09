using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router.Utilities
{
	internal class JsonStringGeneratorUtil
	{
		private delegate void WriteJson<T>(T value, ref Utf8JsonWriter writer);

		public static string FromObject(Dictionary<string, RpcParameter> obj)
		{
			return JsonStringGeneratorUtil.From(obj, JsonStringGeneratorUtil.WriteObject);
		}

		public static string FromArray(RpcParameter[] array)
		{
			return JsonStringGeneratorUtil.From(array, JsonStringGeneratorUtil.WriteArray);
		}

		private static string From<T>(T value, WriteJson<T> writeJsonFunc)
		{
			using (var utf8Stream = new MemoryStream())
			{
				var writer = new Utf8JsonWriter(utf8Stream);
				try
				{
					writeJsonFunc(value, ref writer);

					writer.Flush();

					//Convert to string
					return Encoding.UTF8.GetString(utf8Stream.ToArray());
				}
				finally
				{
					writer.Dispose();
				}
			}
		}


		private static void WriteObject(Dictionary<string, RpcParameter> obj, ref Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			foreach ((string propertyName, RpcParameter value) in obj)
			{
				writer.WritePropertyName(propertyName);
				JsonStringGeneratorUtil.WriteValue(value, ref writer);
			}
			writer.WriteEndObject();
		}

		private static void WriteArray(RpcParameter[] values, ref Utf8JsonWriter writer)
		{
			writer.WriteStartArray();
			foreach (RpcParameter value in values)
			{
				JsonStringGeneratorUtil.WriteValue(value, ref writer);
			}
			writer.WriteEndArray();
		}

		private static void WriteValue(RpcParameter value, ref Utf8JsonWriter writer)
		{
			switch (value.Type)
			{
				case RpcParameterType.Array:
					JsonStringGeneratorUtil.WriteArray(value.GetArrayValue(), ref writer);
					break;
				case RpcParameterType.Null:
					writer.WriteNullValue();
					break;
				case RpcParameterType.Boolean:
					writer.WriteBooleanValue(value.GetBooleanValue());
					break;
				case RpcParameterType.Number:
					RpcNumber number = value.GetNumberValue();
					if(!number.TryGetDecimal(out decimal v))
					{
						throw new NotImplementedException($"Could not parse {number} as a decimal");
					}
					writer.WriteNumberValue(v);
					break;
				case RpcParameterType.String:
					writer.WriteStringValue(value.GetStringValue());
					break;
				case RpcParameterType.Object:
					JsonStringGeneratorUtil.WriteObject(value.GetObjectValue(), ref writer);
					break;
				default:
					throw new NotImplementedException();
			}
		}
	}
}