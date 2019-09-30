using System;
using System.Collections.Generic;
using System.Reflection;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Core;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcRequestMatcher
	{
		/// <summary>
		/// Finds the matching Rpc method for the current request
		/// </summary>
		/// <param name="request">Current Rpc request</param>
		/// <param name="methods">Methods for the current path</param>
		/// <returns>The matching Rpc method to the current request</returns>
		RpcMethodInfo GetMatchingMethod(RpcRequest request, IReadOnlyList<MethodInfo> methods);
	}
}