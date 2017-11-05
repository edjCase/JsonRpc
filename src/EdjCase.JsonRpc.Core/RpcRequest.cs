using System.Collections.Generic;
using EdjCase.JsonRpc.Core.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace EdjCase.JsonRpc.Core
{
	/// <summary>
	/// Model representing a Rpc request
	/// </summary>
	[JsonObject]
	public class RpcRequest
	{
		[JsonConstructor]
		private RpcRequest()
		{

		}

		/// <param name="id">Request id</param>
		/// <param name="method">Target method name</param>
		/// <param name="parameterList">Json parameters for the target method</param>
		public RpcRequest(RpcId id, string method, JToken parameters)
		{
			this.Id = id;
			this.JsonRpcVersion = JsonRpcContants.JsonRpcVersion;
			this.Method = method;
			this.Parameters = parameters;
		}

		/// <summary>
		/// Request Id (Optional)
		/// </summary>
		[JsonProperty("id")]
		[JsonConverter(typeof(RpcIdJsonConverter))]
		public RpcId Id { get; private set; }
		/// <summary>
		/// Version of the JsonRpc to be used (Required)
		/// </summary>
		[JsonProperty("jsonrpc", Required = Required.Always)]
		public string JsonRpcVersion { get; private set; }
		/// <summary>
		/// Name of the target method (Required)
		/// </summary>
		[JsonProperty("method", Required = Required.Always)]
		public string Method { get; private set; }
		/// <summary>
		/// Parameters to invoke the method with (Optional)
		/// </summary>
		[JsonProperty("params")]
		public JToken Parameters { get; private set; }


		public static RpcRequest WithNoParameters(string id, string method)
		{
			return RpcRequest.ConvertInternal(new RpcId(id), method, null, null);
		}

		public static RpcRequest WithNoParameters(string method)
		{
			return RpcRequest.ConvertInternal(default, method, null, null);
		}

		public static RpcRequest WithParameterList(string id, string method, IList<object> parameters, JsonSerializer jsonSerializer = null)
		{
			return RpcRequest.ConvertInternal(new RpcId(id), method, parameters, jsonSerializer);
		}

		public static RpcRequest WithParameterList(int id, string method, IList<object> parameters, JsonSerializer jsonSerializer = null)
		{
			return RpcRequest.ConvertInternal(new RpcId(id), method, parameters, jsonSerializer);
		}

		public static RpcRequest WithParameterList(string method, IList<object> parameters, JsonSerializer jsonSerializer = null)
		{
			return RpcRequest.ConvertInternal(default, method, parameters, jsonSerializer);
		}

		public static RpcRequest WithParameterMap(string id, string method, IDictionary<string, object> parameters, JsonSerializer jsonSerializer = null)
		{
			return RpcRequest.ConvertInternal(new RpcId(id), method, parameters, jsonSerializer);
		}

		public static RpcRequest WithParameterMap(int id, string method, IDictionary<string, object> parameters, JsonSerializer jsonSerializer = null)
		{
			return RpcRequest.ConvertInternal(new RpcId(id), method, parameters, jsonSerializer);
		}

		public static RpcRequest WithParameterMap(string method, IDictionary<string, object> parameters, JsonSerializer jsonSerializer = null)
		{
			return RpcRequest.ConvertInternal(default, method, parameters, jsonSerializer);
		}

		private static RpcRequest ConvertInternal(RpcId id, string method, object parameters, JsonSerializer jsonSerializer = null)
		{
			if(method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}
			JToken sParameters = parameters == null 
				? null
				: jsonSerializer == null
					? JToken.FromObject(parameters)
					: JToken.FromObject(parameters, jsonSerializer);
			return new RpcRequest(id, method, sParameters);
		}
	}
}
