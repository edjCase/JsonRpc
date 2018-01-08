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
		public object[] ConvertedParameters { get; }
		public JToken[] RawParameters { get; }

		public RpcMethodInfo(MethodInfo method, object[] convertedParameters, JToken[] rawParameters)
		{
			this.Method = method;
			this.ConvertedParameters = convertedParameters;
			this.RawParameters = rawParameters;
		}

		public bool HasExactParameterMatch()
		{
			try
			{
				ParameterInfo[] parameters = this.Method.GetParameters();
				for (int i = 0; i < this.RawParameters.Length; i++)
				{
					JToken original = this.RawParameters[i];
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
