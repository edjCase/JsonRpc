using Edjcase.JsonRpc.Router;
using EdjCase.JsonRpc.Core.Utilities;
using EdjCase.JsonRpc.Router.Utilities;
using Newtonsoft.Json.Linq;
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
		public MethodInfo Method { get; }
		public object[] Parameters { get; }

		public RpcMethodInfo(MethodInfo method, object[] parameters)
		{
			this.Method = method;
			this.Parameters = parameters;
		}

		public bool HasExactParameterMatch()
		{
			try
			{
				ParameterInfo[] parameters = this.Method.GetParameters();
				for (int i = 0; i < this.Parameters.Length; i++)
				{
					object original = this.Parameters[i];
					ParameterInfo parameter = parameters[i];
					if (!RpcUtil.TypesMatch(original, parameter.ParameterType))
					{
						return false;
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
