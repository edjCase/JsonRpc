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
		internal JsonSerializerSettings JsonSerializerSettings { get; private set; }


		/// <summary>
		/// If true will show exception messages that the server rpc methods throw. Defaults to false
		/// </summary>
		public bool ShowServerExceptions { get; set; }
		

		/// <summary>
		/// Sets the json serialization settings that will be used in serialization and deserialization
		/// for rpc requests
		/// </summary>
		/// <param name="jsonSerializerSettings"></param>
		public void SetJsonSerializerSettings(JsonSerializerSettings jsonSerializerSettings)
		{
			this.JsonSerializerSettings = jsonSerializerSettings;
		}
	}
}
