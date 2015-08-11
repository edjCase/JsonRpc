using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JsonRpc.Router
{
	internal class RpcMethod
	{
		public RpcRoute Route { get; }
		public string Method => this.methodInfo.Name;
		private MethodInfo methodInfo { get; }
		private Type type { get; }

		private ParameterInfo[] parameterInfoList { get; }

		public RpcMethod(Type type, RpcRoute route, MethodInfo methodInfo)
		{
			this.type = type;
			this.Route = route;
			this.methodInfo = methodInfo;
			this.parameterInfoList = methodInfo.GetParameters();
		}

		public object Invoke(params object[] parameters)
		{
			object obj = Activator.CreateInstance(this.type);
			try
			{
				parameters = this.ConvertParameters(parameters);

				object returnObj = this.methodInfo.Invoke(obj, parameters);
				
				returnObj = RpcMethod.HandleAsyncResponses(returnObj);
				
				return returnObj;
			}
			catch (Exception)
			{
				throw new RpcInvalidParametersException();
			}
		}

		private static object HandleAsyncResponses(object returnObj)
		{
			Task task = returnObj as Task;
			if (task == null) //Not async request
			{
				return returnObj;
			}
			PropertyInfo propertyInfo = task.GetType().GetProperty("Result");
			if (propertyInfo != null)
			{
				//Type of Task<T>. Wait for result then return it
				return propertyInfo.GetValue(returnObj);
			}
			//Just of type Task with no return result
			task.GetAwaiter().GetResult();
			return null;
		}

		private object[] ConvertParameters(object[] parameters)
		{
			if (parameters != null)
			{
				for (int index = 0; index < parameters.Length; index++)
				{
					ParameterInfo parameterInfo = this.parameterInfoList[index];

					if (parameters[index] is string && parameterInfo.ParameterType == typeof (Guid))
					{
						Guid guid;
						Guid.TryParse((string) parameters[index], out guid);
						parameters[index] = guid;
					}
					if (parameters[index] is JObject)
					{
						parameters[index] = ((JObject)parameters[index]).ToObject(parameterInfo.ParameterType);
					}
					if (parameters[index] is JArray)
					{
						parameters[index] = ((JArray)parameters[index]).ToObject(parameterInfo.ParameterType);
					}
					parameters[index] = Convert.ChangeType(parameters[index], parameterInfo.ParameterType);
				}
			}
			return parameters;
		}

		public bool HasParameterSignature(object[] parameterList)
		{
			if(parameterList == null)
			{
				throw new ArgumentNullException(nameof(parameterList));
			}
			if (parameterList.Count() > this.parameterInfoList.Count())
			{
				return false;
			}

			for (int i = 0; i < parameterList.Count(); i++)
			{
				ParameterInfo parameterInfo = this.parameterInfoList[i];
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
				if (parameter is JObject)
				{
					JObject jObject = (JObject)parameter;
					jObject.ToObject(parameterInfo.ParameterType); //Test conversion
					return true;
				}
				if (parameter is JArray)
				{
					JArray jArray = (JArray)parameter;
					jArray.ToObject(parameterInfo.ParameterType); //Test conversion
					return true;
				}
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

		public bool TryParseParameterList(Dictionary<string, object> parametersMap, out object[] parameterList)
		{
			parameterList = new object[this.parameterInfoList.Count()];
			foreach (ParameterInfo parameterInfo in this.parameterInfoList)
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
