using System.Collections.Generic;
using System.Reflection;
using EdjCase.JsonRpc.Core;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcRequestMatcher
	{
		List<RpcMethodInfo> FilterAndBuildMethodInfoByRequest(List<MethodInfo> methods, RpcRequest request);
	}
}