using System;
using Newtonsoft.Json;
using EdjCase.JsonRpc.Router.Abstractions;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Configuration data for the Rpc server that is shared between all middlewares
	/// </summary>
	public class RpcServerConfiguration
	{
		/// <summary>
		/// Json serialization settings that will be used in serialization and deserialization
		/// for rpc requests
		/// </summary>
		public JsonSerializerSettings JsonSerializerSettings { get; set; }


		/// <summary>
		/// If true will show exception messages that the server rpc methods throw. Defaults to false
		/// </summary>
		public bool ShowServerExceptions { get; set; }

		/// <summary>
		/// If greater than 0 the router will throw an error if there is a batch request count
		/// greater than the limit
		/// </summary>
		public int BatchRequestLimit { get; set; }
	}
}
