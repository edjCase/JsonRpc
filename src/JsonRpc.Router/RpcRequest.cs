using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JsonRpc.Router
{
	[JsonObject]
	internal class RpcRequest
	{
		[JsonConstructor]
		private RpcRequest()
		{

		}

		[JsonProperty("id", Required = Required.AllowNull)]
		public string Id { get; private set; }
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpc { get; private set; }
		[JsonProperty("method", Required = Required.Always)]
		public string Method { get; private set; }
		[JsonProperty("params")]
		public object RawParameters { get; private set; }

		[JsonIgnore]
		public object[] ParameterList
		{
			get
			{
				return this.RawParameters as object[];
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
