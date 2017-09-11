using System;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;

namespace EdjCase.JsonRpc.Client
{
	public static class RpcResponseExtensions
	{
		/// <summary>
		/// Parses and returns the result of the rpc response as the type specified. 
		/// Otherwise throws a parsing exception
		/// </summary>
		/// <typeparam name="T">Type of object to parse the response as</typeparam>
		/// <param name="response">Rpc response object</param>
		/// <param name="returnDefaultIfNull">Returns the type's default value if the result is null. Otherwise throws parsing exception</param>
		/// <returns>Result of response as type specified</returns>
		public static T GetResult<T>(this RpcResponse response, bool returnDefaultIfNull = true, JsonSerializerSettings settings = null)
		{
			response.ThrowErrorIfExists();
			if (response.Result == null)
			{
				if(!returnDefaultIfNull && default(T) != null)
				{
					throw new RpcClientParseException($"Unable to convert the result (null) to type '{typeof(T)}'");
				}
				return default(T);
			}
			try
			{
				if(settings == null)
				{
					return response.Result.ToObject<T>();
				}
				else
				{
					JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
					return response.Result.ToObject<T>(jsonSerializer);
				}
			}
			catch (Exception ex)
			{
				throw new RpcClientParseException($"Unable to convert the result to type '{typeof(T)}'", ex);
			}
		}
	}
}
