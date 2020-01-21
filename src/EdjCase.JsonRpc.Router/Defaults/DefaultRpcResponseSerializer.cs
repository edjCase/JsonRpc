using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Defaults
{
	internal class DefaultRpcResponseSerializer : IRpcResponseSerializer
	{
		private IOptions<RpcServerConfiguration> serverConfig { get; }
		public DefaultRpcResponseSerializer(IOptions<RpcServerConfiguration> serverConfig)
		{
			this.serverConfig = serverConfig;
		}

		public Task SerializeBulkAsync(IEnumerable<RpcResponse> responses, Stream stream)
		{
			return this.SerializeInternalAsync(responses, isBulkRequest: true, stream);
		}

		public Task SerializeAsync(RpcResponse response, Stream stream)
		{
			return this.SerializeInternalAsync(new[] { response }, isBulkRequest: false, stream);
		}

		private async Task SerializeInternalAsync(IEnumerable<RpcResponse> responses, bool isBulkRequest, Stream stream)
		{
			var jsonWriter = new Utf8JsonWriter(stream);
			try
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
			finally
			{
				await jsonWriter.FlushAsync();
				await jsonWriter.DisposeAsync();
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

				this.SerializeValue(response.Result!, jsonWriter);
			}
			else
			{
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorPropertyName);
				jsonWriter.WriteStartObject();
				jsonWriter.WriteNumber(JsonRpcContants.ErrorCodePropertyName, response.Error!.Code);
				jsonWriter.WriteString(JsonRpcContants.ErrorMessagePropertyName, response.Error.Message);
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorDataPropertyName);
				this.SerializeValue(response.Error.Data, jsonWriter);
				jsonWriter.WriteEndObject();
			}
			jsonWriter.WriteEndObject();
		}

		private void SerializeValue(object? value, Utf8JsonWriter jsonWriter)
		{
			if (value != null)
			{
				JsonSerializerOptions? options = this.serverConfig.Value.JsonSerializerSettings;

				//TODO a better way? cant figure out how to serialize an object to the writer in an async way
				//JsonSerializer.Serialize(jsonWriter, value, value.GetType(), options) does not work because kestrel doesnt allow non-async calls
				byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), options);
				JsonDocument.Parse(jsonBytes).WriteTo(jsonWriter);
			}
			else
			{
				jsonWriter.WriteNullValue();
			}
		}
	}
}
