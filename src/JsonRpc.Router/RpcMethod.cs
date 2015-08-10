using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JsonRpc.Router
{
	internal class RpcMethod
	{
		public RpcRoute Route { get; }
		public string Method => this.MethodInfo.Name;
		private MethodInfo MethodInfo { get; }
		private Type Type { get; }

		private ParameterInfo[] ParameterInfoList { get; set; }

		public RpcMethod(Type type, RpcRoute route, MethodInfo methodInfo)
		{
			this.Type = type;
			this.Route = route;
			this.MethodInfo = methodInfo;
		}

		public object Invoke(params object[] parameters)
		{
			object obj = Activator.CreateInstance(this.Type);
			try
			{
				return this.MethodInfo.Invoke(obj, parameters);
			}
			catch (Exception)
			{
				throw new InvalidRpcParametersException();
			}
		}
		
		public bool HasParameterSignature(object[] parameterList)
		{
			if(parameterList == null)
			{
				throw new ArgumentNullException(nameof(parameterList));
			}
			if (this.ParameterInfoList == null)
			{
				this.ParameterInfoList = this.MethodInfo.GetParameters();
			}
			if (parameterList.Count() > this.ParameterInfoList.Count())
			{
				return false;
			}

			for (int i = 0; i < parameterList.Count(); i++)
			{
				ParameterInfo parameterInfo = this.ParameterInfoList[i];
				object parameter = parameterList[i];
				if (parameter == null)
				{
					bool isNullable = parameterInfo.HasDefaultValue && parameterInfo.DefaultValue == null;
					if (!isNullable)
					{
						return false;
					}
				}
				bool typeMatches = parameterInfo.ParameterType == parameter.GetType();
				if (!typeMatches)
				{
					return false;
				}
			}
			return true;
		}

		public bool HasParameterSignature(Dictionary<string, object> parametersMap, out object[] parameterList)
		{
			if(parametersMap == null)
			{
				throw new ArgumentNullException(nameof(parametersMap));
			}

			if (this.ParameterInfoList == null)
			{
				this.ParameterInfoList = this.MethodInfo.GetParameters();
			}
			parameterList = new object[this.ParameterInfoList.Count()];
			foreach (ParameterInfo parameterInfo in this.ParameterInfoList)
			{
				if (!parametersMap.ContainsKey(parameterInfo.Name) && !parameterInfo.IsOptional)
				{
					parameterList = null;
					return false;
				}
				parameterList[parameterInfo.Position] = parametersMap[parameterInfo.Name];
			}
			return true;
		}
	}
}
