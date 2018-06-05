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
	public interface IRequestSerializer
	{
		string SerializeBulk(IList<RpcRequest> requests);
		string Serialize(RpcRequest request);
		RpcResponse Deserialize(string json, Type resultType = null);
		List<RpcResponse> DeserializeBulk(string json, Func<RpcId, Type> resultTypeResolver = null);
	}



	public static class JsonSerializerExtensions
	{
		public static RpcResponse<T> Deserialize<T>(this IRequestSerializer jsonSerializer, string json)
		{
			return (RpcResponse<T>)jsonSerializer.Deserialize(json, typeof(T));
		}

		public static List<RpcResponse<T>> DeserializeBulk<T>(this IRequestSerializer jsonSerializer, string json)
		{
			return jsonSerializer.DeserializeBulk(json, id => typeof(T))
				.Cast<RpcResponse<T>>()
				.ToList();
		}

		public static List<RpcResponse> DeserializeBulk(this IRequestSerializer jsonSerializer, string json, IDictionary<RpcId, Type> resultTypeMap)
		{
			return jsonSerializer.DeserializeBulk(json, GetType);

			Type GetType(RpcId id)
			{
				resultTypeMap.TryGetValue(id, out Type value);
				return value;
			}
		}
	}


	public class DefaultRequestJsonSerializer : IRequestSerializer
	{
		private IErrorDataSerializer errorDataSerializer { get; }
		private JsonSerializerSettings jsonSerializerSettings { get; }
		public DefaultRequestJsonSerializer(
			IErrorDataSerializer errorDataSerializer = null,
			JsonSerializerSettings jsonSerializerSettings = null)
		{
			this.errorDataSerializer = errorDataSerializer ?? new DefaultErrorDataSerializer();
			this.jsonSerializerSettings = jsonSerializerSettings;
		}

		public RpcResponse Deserialize(string json, Type resultType = null)
		{
			return (RpcResponse)this.DeserializeInternal(json, isBulk: false, resultTypeResolver: id => resultType);
		}

		public List<RpcResponse> DeserializeBulk(string json, Func<RpcId, Type> resultTypeResolver = null)
		{
			return (List<RpcResponse>)this.DeserializeInternal(json, isBulk: true, resultTypeResolver: resultTypeResolver);
		}

		private object DeserializeInternal(string json, bool isBulk, Func<RpcId, Type> resultTypeResolver = null)
		{
			using (TextReader textReader = new StringReader(json))
			{
				using (JsonReader reader = new JsonTextReader(textReader))
				{
					//Prevent auto date parsing
					reader.DateParseHandling = DateParseHandling.None;

					List<RpcResponse> responses;
					JToken token = JToken.Load(reader);
					switch(token.Type)
					{
						case JTokenType.Array:
							responses = token.Select(a => this.DeserializeResponse(a, resultTypeResolver)).ToList();
							break;
						case JTokenType.Object:
							RpcResponse response = this.DeserializeResponse(token, resultTypeResolver);
							responses = new List<RpcResponse>{response};
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(token.Type));
					}
					if (isBulk)
					{
						return responses;
					}
					return responses.Single();
				}
			}
		}

		public RpcResponse DeserializeResponse(JToken token, Func<RpcId, Type> resultTypeResolver = null)
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
				int code = errorToken.Value<int>(JsonRpcContants.ErrorCodePropertyName);
				string message = errorToken.Value<string>(JsonRpcContants.ErrorMessagePropertyName);
				JToken dataToken = errorToken[JsonRpcContants.ErrorDataPropertyName];

				object data = null;
				if(dataToken != null)
				{
					data = this.errorDataSerializer.Deserialize(code, dataToken.ToString());
				}
				var error = new RpcError(code, message, data: data);
				return new RpcResponse(id, error);
			}
			else
			{
				Type resultType = resultTypeResolver?.Invoke(id);
				object result;
				if (resultType == null)
				{
					//dont deserialize
					//TODO tostring?
					result = token[JsonRpcContants.ResultPropertyName].ToString();
				}
				else if (this.jsonSerializerSettings == null)
				{
					result = token[JsonRpcContants.ResultPropertyName].ToObject(resultType);
				}
				else
				{
					//TODo cache serializer?
					JsonSerializer serializer = JsonSerializer.Create(this.jsonSerializerSettings);
					result = token[JsonRpcContants.ResultPropertyName].ToObject(resultType, serializer);
				}
				return new RpcResponse(id, result, resultType);
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

