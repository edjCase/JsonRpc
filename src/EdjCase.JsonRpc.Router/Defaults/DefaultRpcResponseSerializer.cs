using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router.Defaults
{
	public class DefaultRpcResponseSerializer : IRpcResponseSerializer
	{
		private IOptions<RpcServerConfiguration> serverConfig { get; }
		public DefaultRpcResponseSerializer(IOptions<RpcServerConfiguration> serverConfig)
		{
			this.serverConfig = serverConfig;
		}

		public void SerializeBulk(IEnumerable<RpcResponse> responses, Stream stream)
		{
			this.SerializeInternal(responses, isBulkRequest: true, stream);
		}

		public void Serialize(RpcResponse response, Stream stream)
		{
			this.SerializeInternal(new[] { response }, isBulkRequest: false, stream);
		}

		private void SerializeInternal(IEnumerable<RpcResponse> responses, bool isBulkRequest, Stream stream)
		{
			using (var jsonWriter = new Utf8JsonWriter(stream))
			{
				if (isBulkRequest)
				{
					jsonWriter.WriteStartArray();
					foreach (RpcResponse response in responses)
					{
						this.SerializeResponse(response, jsonWriter);
					}
					jsonWriter.WriteEndArray();
				}
				else
				{
					this.SerializeResponse(responses.Single(), jsonWriter);
				}
			}
		}

		private void SerializeResponse(RpcResponse response, Utf8JsonWriter jsonWriter)
		{
			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyName(JsonRpcContants.IdPropertyName);
			switch (response.Id.Type)
			{
				case RpcIdType.Number:
					jsonWriter.WriteNumberValue(response.Id.NumberValue);
					break;
				case RpcIdType.String:
					jsonWriter.WriteStringValue(response.Id.StringValue);
					break;
				default:
					throw new NotImplementedException();
			}
			jsonWriter.WriteString(JsonRpcContants.VersionPropertyName, "2.0");
			if (!response.HasError)
			{
				jsonWriter.WritePropertyName(JsonRpcContants.ResultPropertyName);

				this.SerializeValue(response.Result, jsonWriter);
			}
			else
			{
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorPropertyName);
				jsonWriter.WriteStartObject();
				jsonWriter.WriteNumber(JsonRpcContants.ErrorCodePropertyName, response.Error.Code);
				jsonWriter.WriteString(JsonRpcContants.ErrorMessagePropertyName, response.Error.Message);
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorDataPropertyName);
				this.SerializeValue(response.Error.Data, jsonWriter);
				jsonWriter.WriteEndObject();
			}
			jsonWriter.WriteEndObject();
		}

		private void SerializeValue(object value, Utf8JsonWriter jsonWriter)
		{
			if (value != null)
			{
				JsonSerializerOptions options = this.serverConfig.Value.JsonSerializerSettings;
				JsonSerializer.Serialize(jsonWriter, value, value.GetType(), options);
			}
			else
			{
				jsonWriter.WriteNullValue();
			}
		}
	}
}
