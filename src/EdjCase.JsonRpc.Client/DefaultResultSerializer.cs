using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Client
{
	public interface IResultSerializer
	{
		object Deserialize(string json, Type type = null);
		string Serialize(object value);
	}

	public class DefaultResultSerializer : IResultSerializer
	{
		private JsonSerializerSettings jsonSerializerSettings { get; }
		public DefaultResultSerializer(JsonSerializerSettings jsonSerializerSettings = null)
		{
			this.jsonSerializerSettings = jsonSerializerSettings;
		}

		public object Deserialize(string json, Type type = null)
		{
			if (type == null)
			{
				//dont deserialize
				return json;
			}
			if (this.jsonSerializerSettings == null)
			{
				return JsonConvert.DeserializeObject(json, type);
			}
			return JsonConvert.DeserializeObject(json, type, this.jsonSerializerSettings);
		}

		public string Serialize(object value)
		{
			if (this.jsonSerializerSettings == null)
			{
				return JsonConvert.SerializeObject(value);
			}
			return JsonConvert.SerializeObject(value, this.jsonSerializerSettings);
		}
	}
}
