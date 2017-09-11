using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public class RpcRouteInfo
	{
		public Type Controller { get; }
		public string MethodName { get; }
		public object[] Parameters { get; }
		public RpcPath Path { get; }

		internal RpcRouteInfo(Type controller, string methodName, object[] paramters, RpcPath path)
		{
			this.Controller = controller;
			this.MethodName = methodName;
			this.Parameters = paramters;
			this.Path = path;
		}
	}
}
