using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JsonRpc.Router
{
	[JsonObject]
	public class RpcRequest
	{
		[JsonConstructor]
		private RpcRequest()
		{

		}

		public RpcRequest(string id, string jsonRpcVersion, string method, params object[] parameterList)
		{
			this.Id = id;
			this.JsonRpcVersion = jsonRpcVersion;
			this.Method = method;
			this.RawParameters = parameterList;
		}

		public RpcRequest(string id, string jsonRpcVersion, string method, Dictionary<string, object> parameterMap)
		{
			this.Id = id;
			this.JsonRpcVersion = jsonRpcVersion;
			this.Method = method;
			this.RawParameters = parameterMap;
		}

		[JsonProperty("id")]
		public object Id { get; private set; }
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpcVersion { get; private set; }
		[JsonProperty("method", Required = Required.Always)]
		public string Method { get; private set; }
		[JsonProperty("params")]
		public object RawParameters { get; private set; }

		[JsonIgnore]
		public object[] ParameterList
		{
			get
			{
				var parameterList = this.RawParameters as object[];
				if (parameterList == null)
				{
					JArray jObject = this.RawParameters as JArray;
					if (jObject != null)
					{
						this.RawParameters = jObject.ToObject<object[]>();
						return (object[]) this.RawParameters;
					}
				}
				return parameterList;
			}
		}

		[JsonIgnore]
		public Dictionary<string, object> ParameterMap
		{
			get
			{
				var parameterMap = this.RawParameters as Dictionary<string, object>;
				if (parameterMap == null)
				{
					JObject jObject = this.RawParameters as JObject;
					if (jObject != null)
					{
						this.RawParameters = jObject.ToObject<Dictionary<string, object>>();
						return this.RawParameters as Dictionary<string, object>;
					}
				}
				return parameterMap;
			}
		}
	}
}
