using System;
using System.Collections.Generic;
using System.Reflection;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcRequestMatcher
	{
		/// <summary>
		/// Finds the matching Rpc method for the current request
		/// </summary>
		/// <param name="request">Current Rpc request</param>
		/// <returns>The matching Rpc method to the current request</returns>
		IRpcMethodInfo GetMatchingMethod(RpcRequestSignature requestSignature);
	}
}