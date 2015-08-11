using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JsonRpc.Router
{
	public class RpcIdJsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			value = this.ValidateValue(value);
			writer.WriteValue(value);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return this.ValidateValue(reader.Value);
		}

		private object ValidateValue(object value)
		{
			if (value == null)
			{
				return null;
			}
			if(!this.CanConvert(value.GetType()))
			{
				throw new RpcInvalidRequestException("Id must be a string or a number.");
			}
			string idString = value as string;
			if (idString != null && string.IsNullOrWhiteSpace(idString))
			{
				value = null; //If just empty or whitespace id should be null
			}
			return value;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof (string) || this.IsNumericType(objectType);
		}

		private bool IsNumericType(Type type)
		{
			return type == typeof (long)
					|| type == typeof (int)
					|| type == typeof (short)
					|| type == typeof (float)
					|| type == typeof (double)
					|| type == typeof (decimal);
		}
	}
}
