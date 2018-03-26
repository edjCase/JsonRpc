using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Router.Defaults
{
	public class DefaultRpcResponseSerializer : IRpcResponseSerializer
	{
		private IOptions<RpcServerConfiguration> serverConfig { get; }
		public DefaultRpcResponseSerializer(IOptions<RpcServerConfiguration> serverConfig)
		{
			this.serverConfig = serverConfig;
		}

		public string SerializeBulk(IEnumerable<RpcResponse> responses)
		{
			return this.SerializeInternal(responses, isBulkRequest: true);
		}

		public string Serialize(RpcResponse response)
		{
			return this.SerializeInternal(new[] { response }, isBulkRequest: false);
		}

		private string SerializeInternal(IEnumerable<RpcResponse> responses, bool isBulkRequest)
		{
			using (StringWriter textWriter = new StringWriter())
			{
				using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
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
				return textWriter.ToString();
			}

		}

		private void SerializeResponse(RpcResponse response, JsonTextWriter jsonWriter)
		{
			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyName(JsonRpcContants.IdPropertyName);
			jsonWriter.WriteValue(response.Id.Value);
			jsonWriter.WritePropertyName(JsonRpcContants.VersionPropertyName);
			jsonWriter.WriteValue("2.0");
			if (!response.HasError)
			{
				jsonWriter.WritePropertyName(JsonRpcContants.ResultPropertyName);

				this.SerializeValue(response.Result, jsonWriter);
			}
			else
			{
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorPropertyName);
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorCodePropertyName);
				jsonWriter.WriteValue(response.Error.Code);
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorMessagePropertyName);
				jsonWriter.WriteValue(response.Error.GetMessage(this.serverConfig?.Value?.ShowServerExceptions ?? false));
				jsonWriter.WritePropertyName(JsonRpcContants.ErrorDataPropertyName);
				this.SerializeValue(response.Error.Data, jsonWriter);
			}
			jsonWriter.WriteEndObject();
		}

		private void SerializeValue(object value, JsonTextWriter jsonWriter)
		{
			if (value != null)
			{
				string valueJson = JsonConvert.SerializeObject(value, this.serverConfig.Value.JsonSerializerSettings);
				jsonWriter.WriteRawValue(valueJson);
			}
			else
			{
				jsonWriter.WriteNull();
			}
		}
	}
}
