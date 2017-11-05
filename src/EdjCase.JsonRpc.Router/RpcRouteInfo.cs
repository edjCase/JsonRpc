using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public class RpcRouteInfo
	{
		public RpcMethodInfo MethodInfo { get; }
		public RpcPath Path { get; }

		internal RpcRouteInfo(RpcMethodInfo methodInfo, RpcPath path)
		{
			this.MethodInfo = methodInfo;
			this.Path = path;
		}
	}
}
