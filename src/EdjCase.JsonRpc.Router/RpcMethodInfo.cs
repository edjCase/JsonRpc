using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public class RpcMethodInfo
	{
		public MethodInfo Method { get; }
		public object[] ConvertedParameters { get; }
		public object[] RawParameters { get; }

		public RpcMethodInfo(MethodInfo method, object[] convertedParameters, object[] rawParameters)
		{
			this.Method = method;
			this.ConvertedParameters = convertedParameters;
		}
	}
}
