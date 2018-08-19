using System;
using System.Collections.Generic;
using System.Text;
using EdjCase.JsonRpc.Core;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcResponseSerializer
	{
		string Serialize(RpcResponse response);
		string SerializeBulk(IEnumerable<RpcResponse> responses);
	}
}
