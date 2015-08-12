using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace edjCase.JsonRpc.Router.JsonConverters
{
	public class RpcParametersJsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(value);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			switch (reader.TokenType)
			{
				case JsonToken.StartObject:
					try
					{
						JObject jObject = JObject.Load(reader);
						return jObject.ToObject<Dictionary<string, object>>();
					}
					catch (Exception)
					{
						throw new RpcInvalidRequestException("Request parameters can only be an associative array, list or null.");
					}
				case JsonToken.StartArray:
					return JArray.Load(reader).ToObject<object[]>();
				case JsonToken.Null:
					return null;
			}
			throw new RpcInvalidRequestException("Request parameters can only be an associative array, list or null.");
		}

		public override bool CanConvert(Type objectType)
		{
			return true;
		}
	}
}
