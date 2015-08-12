using System.Collections.Generic;
using edjCase.JsonRpc.Router.JsonConverters;
using Newtonsoft.Json;

namespace edjCase.JsonRpc.Router
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
		[JsonConverter(typeof(RpcIdJsonConverter))]
		public object Id { get; private set; }
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpcVersion { get; private set; }
		[JsonProperty("method", Required = Required.Always)]
		public string Method { get; private set; }
		[JsonProperty("params")]
		[JsonConverter(typeof(RpcParametersJsonConverter))]
		public object RawParameters { get; private set; }

		[JsonIgnore]
		public object[] ParameterList => this.RawParameters as object[];

		[JsonIgnore]
		public Dictionary<string, object> ParameterMap => this.RawParameters as Dictionary<string, object>;
	}
}
