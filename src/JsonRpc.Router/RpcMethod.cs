using System;
using System.Collections.Generic;
using System.Globalization;
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
				if (parameters != null)
				{
					ParameterInfo[] parameterInfoList = this.MethodInfo.GetParameters();
					for (int index = 0; index < parameters.Length; index++)
					{
						ParameterInfo parameterInfo = parameterInfoList[index];

						if (parameters[index] is string && parameterInfo.ParameterType == typeof(Guid))
						{
							Guid guid;
							Guid.TryParse((string)parameters[index], out guid);
							parameters[index] = guid;
						}
						parameters[index] = Convert.ChangeType(parameters[index], parameterInfo.ParameterType);
					}
				}
				return this.MethodInfo.Invoke(obj, parameters);
			}
			catch (Exception)
			{
				throw new RpcInvalidParametersException();
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
				bool isMatch = RpcMethod.ParameterMatches(parameterInfo, parameter);
				if (!isMatch)
				{
					return false;
				}
			}
			return true;
		}

		private static bool ParameterMatches(ParameterInfo parameterInfo, object parameter)
		{
			if (parameter == null)
			{
				bool isNullable = parameterInfo.HasDefaultValue && parameterInfo.DefaultValue == null;
				return isNullable;
			}
			if (parameterInfo.ParameterType == parameter.GetType())
			{
				return true;
			}
			if (parameter is long)
			{
				return parameterInfo.ParameterType == typeof (short) 
					|| parameterInfo.ParameterType == typeof (int);
			}
			if (parameter is double || parameter is decimal)
			{
				return parameterInfo.ParameterType == typeof(double) 
					|| parameterInfo.ParameterType == typeof(decimal) 
					|| parameterInfo.ParameterType == typeof(float);
			}
			if (parameter is string && parameterInfo.ParameterType == typeof (Guid))
			{
				Guid guid;
				return Guid.TryParse((string)parameter, out guid);
			}
			try
			{
				//Final check to see if the conversion can happen
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				Convert.ChangeType(parameter, parameterInfo.ParameterType);
			}
			catch (Exception)
			{
				return false;
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
			bool canParse = this.TryParseParameterList(parametersMap, out parameterList);
			if (!canParse)
			{
				return false;
			}
			bool hasSignature = this.HasParameterSignature(parameterList);
			if (hasSignature)
			{
				return true;
			}
			parameterList = null;
			return false;
		}

		private bool TryParseParameterList(Dictionary<string, object> parametersMap, out object[] parameterList)
		{
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
