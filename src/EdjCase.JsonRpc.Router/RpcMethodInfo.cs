using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common.Utilities;
using EdjCase.JsonRpc.Router.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public class RpcMethodInfo
	{
		public MethodInfo MethodInfo { get; }
		public RpcParameterInfo[] Parameters { get; }

		public RpcMethodInfo(MethodInfo methodInfo, RpcParameterInfo[] parameters)
		{
			this.MethodInfo = methodInfo;
			this.Parameters = parameters;
		}
	}

	public class RpcParameterInfo
	{
		public string Name { get; }
		public RpcParameterType Type { get; }
		public Type RawType { get; }
		public bool IsOptional { get; }

		public RpcParameterInfo(string name, RpcParameterType type, Type rawType, bool isOptional)
		{
			this.Name = name;
			this.Type = type;
			this.RawType = rawType;
			this.IsOptional = isOptional;
		}
	}
}
