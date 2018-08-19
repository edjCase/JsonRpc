using System.Collections.Generic;
using System.Linq;
using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace EdjCase.JsonRpc.Core
{
	/// <summary>
	/// Model representing a Rpc request
	/// </summary>
	public class RpcRequest
	{
		/// <param name="id">Request id</param>
		/// <param name="method">Target method name</param>
		/// <param name="parameterList">Json parameters for the target method</param>
		public RpcRequest(RpcId id, string method, RpcParameters parameters = default)
		{
			this.Id = id;
			this.Method = method;
			this.Parameters = parameters;
		}
		
		/// <param name="method">Target method name</param>
		/// <param name="parameterList">Json parameters for the target method</param>
		public RpcRequest(string method, RpcParameters parameters = default)
		{
			this.Id = null;
			this.Method = method;
			this.Parameters = parameters;
		}

		/// <summary>
		/// Request Id (Optional)
		/// </summary>
		public RpcId Id { get; private set; }
		/// <summary>
		/// Name of the target method (Required)
		/// </summary>
		public string Method { get; private set; }
		/// <summary>
		/// Parameters to invoke the method with (Optional)
		/// </summary>
		public RpcParameters Parameters { get; private set; }

		public static RpcRequest WithNoParameters(string method, RpcId id = default)
		{
			return RpcRequest.WithParameters(method, default, id);
		}

		public static RpcRequest WithParameterList(string method, IList<object> parameterList, RpcId id = default)
		{
			parameterList = parameterList ?? new object[0];
			RpcParameters parameters = RpcParameters.FromList(parameterList);
			return RpcRequest.WithParameters(method, parameters, id);
		}

		public static RpcRequest WithParameterMap(string method, IDictionary<string, object> parameterDictionary, RpcId id = default)
		{
			parameterDictionary = parameterDictionary ?? new Dictionary<string, object>();
			RpcParameters parameters = RpcParameters.FromDictionary(parameterDictionary);
			return RpcRequest.WithParameters(method, parameters, id);
		}

		public static RpcRequest WithParameters(string method, RpcParameters parameters, RpcId id = default)
		{
			if(method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}
			return new RpcRequest(id, method, parameters);
		}
	}
}
