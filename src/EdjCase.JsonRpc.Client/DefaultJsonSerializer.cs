using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Client
{
	public interface IJsonSerializer
	{
		string SerializeBulk(IList<RpcRequest> requests);
		string Serialize(RpcRequest request);
		List<RpcResponse> DeserializeBulk(string json);
		RpcResponse Deserialize(string json);
	}
	public class DefaultJsonSerializer : IJsonSerializer
	{
		private JsonSerializerSettings jsonSerializerSettings { get; }
		public DefaultJsonSerializer(JsonSerializerSettings jsonSerializerSettings = null)
		{
			this.jsonSerializerSettings = jsonSerializerSettings;
		}

		public RpcResponse Deserialize(string json)
		{
			return (RpcResponse)this.DeserializeInternal(json, isBulk: false);
		}

		public List<RpcResponse> DeserializeBulk(string json)
		{
			return (List<RpcResponse>)this.DeserializeInternal(json, isBulk: true);
		}

		private object DeserializeInternal(string json, bool isBulk)
		{
			using (TextReader textReader = new StringReader(json))
			{
				using (JsonReader reader = new JsonTextReader(textReader))
				{
					//Prevent auto date parsing
					reader.DateParseHandling = DateParseHandling.None;

					if (isBulk)
					{
						JArray array = JArray.Load(reader);
						return array.Select(this.DeserializeResponse).ToList();
					}
					else
					{
						JObject jToken = JObject.Load(reader);
						return this.DeserializeResponse(jToken);
					}
				}
			}
		}

		public RpcResponse DeserializeResponse(JToken token)
		{
			RpcId id = null;
			JToken idToken = token[JsonRpcContants.IdPropertyName];
			if (idToken == null)
			{
				throw new RpcClientParseException("Unable to parse request id.");
			}
			switch (idToken.Type)
			{
				case JTokenType.Null:
					break;
				case JTokenType.Integer:
				case JTokenType.Float:
					id = new RpcId(idToken.Value<double>());
					break;
				case JTokenType.String:
				case JTokenType.Guid:
					id = new RpcId(idToken.Value<string>());
					break;
				default:
					throw new RpcClientParseException("Unable to parse rpc id as string or number.");
			}
			JToken errorToken = token[JsonRpcContants.ErrorPropertyName];
			if (errorToken != null)
			{
				int code = token.Value<int>(JsonRpcContants.ErrorCodePropertyName);
				string message = token.Value<string>(JsonRpcContants.ErrorMessagePropertyName);
				JToken data = token[JsonRpcContants.ErrorDataPropertyName];
				var error = new RpcError(code, message, data: data);
				return new RpcResponse(id, error);
			}
			else
			{
				JToken result = token[JsonRpcContants.ResultPropertyName];
				return new RpcResponse(id, result);
			}
		}

		public string Serialize(RpcRequest request)
		{
			return this.SerializeInternal(new[] { request }, isBulkRequest: false);
		}

		public string SerializeBulk(IList<RpcRequest> requests)
		{
			return this.SerializeInternal(requests, isBulkRequest: true);
		}

		private string SerializeInternal(IEnumerable<RpcRequest> requests, bool isBulkRequest)
		{
			using (StringWriter textWriter = new StringWriter())
			{
				using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
				{
					if (isBulkRequest)
					{
						jsonWriter.WriteStartArray();
						foreach (RpcRequest request in requests)
						{
							this.SerializeRequest(request, jsonWriter);
						}
						jsonWriter.WriteEndArray();
					}
					else
					{
						this.SerializeRequest(requests.Single(), jsonWriter);
					}
				}
				return textWriter.ToString();
			}

		}

		private void SerializeRequest(RpcRequest request, JsonTextWriter jsonWriter)
		{
			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyName(JsonRpcContants.IdPropertyName);
			jsonWriter.WriteValue(request.Id.Value);
			jsonWriter.WritePropertyName(JsonRpcContants.VersionPropertyName);
			jsonWriter.WriteValue("2.0");
			jsonWriter.WritePropertyName(JsonRpcContants.MethodPropertyName);
			jsonWriter.WriteValue(request.Method);
			jsonWriter.WritePropertyName(JsonRpcContants.ParamsPropertyName);
			if (!request.Parameters.HasValue)
			{
				//empty arrray
				jsonWriter.WriteStartArray();
				jsonWriter.WriteEndArray();
			}
			else
			{
				switch (request.Parameters.Type)
				{
					case RpcParametersType.Array:
						jsonWriter.WriteStartArray();
						foreach (object value in request.Parameters.ArrayValue)
						{
							this.SerializeValue(value, jsonWriter);
						}
						jsonWriter.WriteEndArray();
						break;
					case RpcParametersType.Dictionary:
						jsonWriter.WriteEndObject();
						foreach (KeyValuePair<string, object> value in request.Parameters.DictionaryValue)
						{
							jsonWriter.WritePropertyName(value.Key);
							this.SerializeValue(value.Value, jsonWriter);
						}
						jsonWriter.WriteEndObject();
						break;
				}
			}
			jsonWriter.WriteEndObject();
		}


		private void SerializeValue(object value, JsonTextWriter jsonWriter)
		{
			if (value != null)
			{
				string valueJson;
				if (this.jsonSerializerSettings == null)
				{
					valueJson = JsonConvert.SerializeObject(value);
				}
				else
				{
					valueJson = JsonConvert.SerializeObject(value, this.jsonSerializerSettings);

				}
				jsonWriter.WriteRawValue(valueJson);
			}
			else
			{
				jsonWriter.WriteNull();
			}
		}

	}
}

