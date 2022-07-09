using EdjCase.JsonRpc.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Client
{
	internal interface IRequestSerializer
	{
		string SerializeBulk(IList<RpcRequest> requests);
		string Serialize(RpcRequest request);
		List<RpcResponse> Deserialize(string json, IDictionary<RpcId, Type> typeMap);
	}


	public class DefaultRequestJsonSerializer : IRequestSerializer
	{
		private JsonSerializerSettings? jsonSerializerSettings { get; }
		private IDictionary<int, Type>? errorTypes { get; }
		internal DefaultRequestJsonSerializer(
			JsonSerializerSettings? jsonSerializerSettings = null,
			IDictionary<int, Type>? errorTypes = null)
		{
			this.jsonSerializerSettings = jsonSerializerSettings;
			this.errorTypes = errorTypes;
		}

		public List<RpcResponse> Deserialize(string json, IDictionary<RpcId, Type> typeMap)
		{
			using TextReader textReader = new StringReader(json);
			using JsonReader reader = new JsonTextReader(textReader)
			{
				//Prevent auto date parsing
				DateParseHandling = DateParseHandling.None
			};

			List<RpcResponse> responses;
			JToken token = JToken.Load(reader);
			switch (token.Type)
			{
				case JTokenType.Array:
					responses = token.Select(Deserialize).ToList();
					RpcResponse Deserialize(JToken t)
					{
						return this.DeserializeResponse(t, typeMap);
					}
					break;
				case JTokenType.Object:
					RpcResponse response = this.DeserializeResponse(token, typeMap);
					responses = new List<RpcResponse> { response };
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(token.Type));
			}
			return responses;
		}

		public RpcResponse DeserializeResponse(JToken token, IDictionary<RpcId, Type> typeMap)
		{
			JToken? idToken = token[JsonRpcContants.IdPropertyName];
			if (idToken == null)
			{
				throw new RpcClientParseException("Unable to parse request id.");
			}
			RpcId id;
			switch (idToken.Type)
			{
				case JTokenType.Null:
					id = new RpcId();
					break;
				case JTokenType.Integer:
					id = new RpcId(idToken.Value<long>()!);
					break;
				case JTokenType.String:
				case JTokenType.Guid:
					id = new RpcId(idToken.Value<string>()!);
					break;
				default:
					throw new RpcClientParseException("Unable to parse rpc id as string or integer.");
			}
			if(!typeMap.TryGetValue(id, out Type type))
			{
				throw new RpcClientParseException("Unable to detect result type, cannot deserialize.");
			}
			JToken? errorToken = token[JsonRpcContants.ErrorPropertyName];
			if (errorToken != null && errorToken.HasValues)
			{
				int code = errorToken.Value<int>(JsonRpcContants.ErrorCodePropertyName);
				string message = errorToken.Value<string>(JsonRpcContants.ErrorMessagePropertyName)!;
				JToken? dataToken = errorToken[JsonRpcContants.ErrorDataPropertyName];

				object? data = null;
				if (dataToken != null)
				{
					if (this.errorTypes != null && this.errorTypes.TryGetValue(code, out Type errorCodeType))
					{
						data = dataToken.ToObject(errorCodeType);
					}
					else
					{
						data = dataToken.ToString();
					}
				}
				var error = new RpcError(code, message, data: data);
				return new RpcResponse(id, error);
			}
			else
			{
				object? result;
				if (this.jsonSerializerSettings == null)
				{
					result = token[JsonRpcContants.ResultPropertyName]?.ToObject(type);
				}
				else
				{
					//TODo cache serializer?
					JsonSerializer serializer = JsonSerializer.Create(this.jsonSerializerSettings);
					result = token[JsonRpcContants.ResultPropertyName]?.ToObject(type, serializer);
				}
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
			using StringWriter textWriter = new StringWriter();
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
						jsonWriter.WriteStartObject();
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

