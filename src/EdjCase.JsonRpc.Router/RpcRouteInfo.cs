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
		public IServiceProvider RequestServices { get; }

		internal RpcRouteInfo(RpcMethodInfo methodInfo, RpcPath path, IServiceProvider requestServices)
		{
			this.MethodInfo = methodInfo;
			this.Path = path;
			this.RequestServices = requestServices;
		}
	}
}
