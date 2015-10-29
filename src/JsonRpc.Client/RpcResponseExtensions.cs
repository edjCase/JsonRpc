using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using edjCase.JsonRpc.Core;
using Newtonsoft.Json.Linq;

namespace edjCase.JsonRpc.Client
{
    public static class RpcResponseExtensions
    {
		public static T GetResult<T>(this RpcResponse response)
		{
			if (response.Result == null)
			{
				return default(T);
			}
			if (response.Result is T)
			{
				return (T)response.Result;
			}
			JObject jObject = response.Result as JObject;
			if (jObject != null)
			{
				return jObject.ToObject<T>();
			}
			JArray jArray = response.Result as JArray;
			if (jArray != null)
			{
				return jArray.ToObject<T>();
			}
			throw new RpcClientParseException($"Unable to convert the result of type '{response.Result.GetType()}' to type '{typeof(T)}'");
		}
	}
}
