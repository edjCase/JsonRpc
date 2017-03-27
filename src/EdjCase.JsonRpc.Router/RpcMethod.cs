using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Object that represents a preconfigured method that the Rpc Api allows a request to call
	/// </summary>
	internal class RpcMethod
	{
		/// <summary>
		/// The method's configured request route it can be called from
		/// </summary>
		public RpcRoute Route { get; }
		/// <summary>
		/// Authorize data list that will be checked for the method for authorization.
		/// If empty, no authorization will be run
		/// </summary>
		public List<IAuthorizeData> AuthorizeDataListMethod { get; }
		/// <summary>
		/// Authorize data list that will be checked for the class for authorization.
		/// If empty, no authorization will be run
		/// </summary>
		public List<IAuthorizeData> AuthorizeDataListClass { get; }
		/// <summary>
		/// If true, bypasses authorization check
		/// </summary>
		public bool AllowAnonymousOnMethod { get; }
		/// <summary>
		/// If true, bypasses authorization check
		/// </summary>
		public bool AllowAnonymousOnClass { get; }
		/// <summary>
		/// The name of the method
		/// </summary>
		public string Method => this.methodInfo.Name;
		/// <summary>
		/// Reflection information about the method
		/// </summary>
		private MethodInfo methodInfo { get; }
		/// <summary>
		/// The class the method exists in 
		/// </summary>
		private Type type { get; }
		/// <summary>
		/// Reflection information about each of the method's parameters
		/// </summary>
		private ParameterInfo[] parameterInfoList { get; }
		//TODO logger?
		private ILogger<RpcMethod> logger { get; }

		/// <summary>
		/// Service provider to be used as an IoC Container. If not set it will use
		/// basic reflection to invoke methods
		/// </summary>
		private IServiceProvider serviceProvider { get; }

		/// <summary>
		/// Json serialization settings that will be used in serialization and deserialization
		/// for rpc requests
		/// </summary>
		private JsonSerializerSettings jsonSerializerSettings { get; }

		/// <param name="type">Class type that the method is in</param>
		/// <param name="route">Request route the method can be called from</param>
		/// <param name="methodInfo">Reflection information about the method</param>
		/// <param name="serviceProvider">(Optional) Service provider to be used as an IoC Container</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		public RpcMethod(Type type, RpcRoute route, MethodInfo methodInfo, IServiceProvider serviceProvider = null, JsonSerializerSettings jsonSerializerSettings = null,
			ILogger<RpcMethod> logger = null)
		{
			this.type = type;
			this.Route = route;
			this.methodInfo = methodInfo;
			this.parameterInfoList = methodInfo.GetParameters();
			this.serviceProvider = serviceProvider;
			this.jsonSerializerSettings = jsonSerializerSettings;
			IEnumerable<Attribute> customClassAttributes = type.GetTypeInfo().GetCustomAttributes();
			this.AuthorizeDataListClass = customClassAttributes.OfType<IAuthorizeData>().ToList();
			this.AllowAnonymousOnClass = customClassAttributes.OfType<IAllowAnonymous>().Any();
			IEnumerable<Attribute> customMethodAttributes = this.methodInfo.GetCustomAttributes();
			this.AuthorizeDataListMethod = customMethodAttributes.OfType<IAuthorizeData>().ToList();
			this.AllowAnonymousOnMethod = customMethodAttributes.OfType<IAllowAnonymous>().Any();
			this.logger = logger;
		}

		/// <summary>
		/// Invokes the method with the specified parameters, returns the result of the method
		/// </summary>
		/// <exception cref="RpcInvalidParametersException">Thrown when conversion of parameters fails or when invoking the method is not compatible with the parameters</exception>
		/// <param name="parameters">List of parameters to invoke the method with</param>
		/// <returns>The result of the invoked method</returns>
		public async Task<object> InvokeAsync(params object[] parameters)
		{
			object obj = null;
			if (this.serviceProvider != null)
			{
				//Use service provider (if exists) to create instance
				var objectFactory = ActivatorUtilities.CreateFactory(this.type, new Type[0]);
				obj = objectFactory(this.serviceProvider, null);
			}
			if (obj == null)
			{
				//Use reflection to create instance if service provider failed or is null
				obj = Activator.CreateInstance(this.type);
			}
			try
			{
				parameters = this.ConvertParameters(parameters);

				object returnObj = this.methodInfo.Invoke(obj, parameters);

				returnObj = await RpcMethod.HandleAsyncResponses(returnObj);

				return returnObj;
			}
			catch (TargetInvocationException ex)
			{
				throw new RpcUnknownException("Exception occurred from target method execution.", ex);
			}
			catch (Exception ex)
			{
				throw new RpcInvalidParametersException("Exception from attempting to invoke method. Possibly invalid parameters for method.", ex);
			}
		}

		/// <summary>
		/// Handles/Awaits the result object if it is a async Task
		/// </summary>
		/// <param name="returnObj">The result of a invoked method</param>
		/// <returns>Awaits a Task and returns its result if object is a Task, otherwise returns the same object given</returns>
		private static async Task<object> HandleAsyncResponses(object returnObj)
		{
			Task task = returnObj as Task;
			if (task == null) //Not async request
			{
				return returnObj;
			}
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				throw new TargetInvocationException(ex);
			}
			PropertyInfo propertyInfo = task.GetType().GetProperty("Result");
			if (propertyInfo != null)
			{
				//Type of Task<T>. Wait for result then return it
				return propertyInfo.GetValue(returnObj);
			}
			//Just of type Task with no return result			
			return null;
		}

		/// <summary>
		/// Converts the object array into the exact types the method needs (e.g. long -> int)
		/// </summary>
		/// <param name="parameters">Array of parameters for the method</param>
		/// <returns>Array of objects with the exact types required by the method</returns>
		private object[] ConvertParameters(object[] parameters)
		{
			if (parameters != null)
			{
				for (int index = 0; index < parameters.Length; index++)
				{
					ParameterInfo parameterInfo = this.parameterInfoList[index];
					parameters[index] = this.ConvertParameter(parameterInfo.ParameterType, parameters[index]);
				}
			}
			return parameters;
		}

		private object ConvertParameter(Type parameterType, object parameterValue)
		{
			if (parameterValue == null)
			{
				return null;
			}
			//Missing type is for optional parameters
			if (parameterValue is Missing)
			{
				return parameterValue;
			}
			Type nullableType = Nullable.GetUnderlyingType(parameterType);
			if (nullableType != null)
			{
				return this.ConvertParameter(nullableType, parameterValue);
			}
			if (parameterValue is string && parameterType == typeof(Guid))
			{
				Guid guid;
				Guid.TryParse((string)parameterValue, out guid);
				return guid;
			}
			if (parameterType.GetTypeInfo().IsEnum)
			{
				if (parameterValue is string)
				{
					return Enum.Parse(parameterType, (string)parameterValue);
				}
				else if (parameterValue is long)
				{
					return Enum.ToObject(parameterType, parameterValue);
				}
			}
			if (parameterValue is JObject)
			{
				JsonSerializer jsonSerializer = JsonSerializer.Create(this.jsonSerializerSettings);
				return ((JObject)parameterValue).ToObject(parameterType, jsonSerializer);
			}
			if (parameterValue is JArray)
			{
				JsonSerializer jsonSerializer = JsonSerializer.Create(this.jsonSerializerSettings);
				return ((JArray)parameterValue).ToObject(parameterType, jsonSerializer);
			}
			return Convert.ChangeType(parameterValue, parameterType);
		}

		/// <summary>
		/// Detects if list of parameters matches the method signature
		/// </summary>
		/// <param name="parameterList">Array of parameters for the method</param>
		/// <returns>True if the method signature matches the parameterList, otherwise False</returns>
		public bool HasParameterSignature(object[] parameterList, out object[] correctedParameterList)
		{
			correctedParameterList = parameterList ?? throw new ArgumentNullException(nameof(parameterList));
			if (parameterList.Count() > this.parameterInfoList.Count())
			{
				return false;
			}

			for (int i = 0; i < this.parameterInfoList.Count(); i++)
			{
				ParameterInfo parameterInfo = this.parameterInfoList[i];
				if (parameterList.Count() <= i)
				{
					if (!parameterInfo.IsOptional)
					{
						return false;
					}
					correctedParameterList = new object[correctedParameterList.Length + 1];
					correctedParameterList[correctedParameterList.Length - 1] = Type.Missing;
				}
				else
				{
					object parameter = parameterList[i];
					bool isMatch = this.ParameterMatches(parameterInfo, parameter);
					if (!isMatch)
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Detects if the request parameter matches the method parameter
		/// </summary>
		/// <param name="parameterInfo">Reflection info about a method parameter</param>
		/// <param name="value">The request's value for the parameter</param>
		/// <returns>True if the request parameter matches the type of the method parameter</returns>
		private bool ParameterMatches(ParameterInfo parameterInfo, object value)
		{
			Type parameterType = parameterInfo.ParameterType;
			Type nullableType = Nullable.GetUnderlyingType(parameterType);
			if (value == null)
			{
				bool isNullable = nullableType != null
					|| parameterType.GetTypeInfo().IsClass
					|| (parameterInfo.HasDefaultValue && parameterInfo.DefaultValue == null);
				return isNullable;
			}
			if (parameterType == value.GetType())
			{
				return true;
			}
			if (nullableType != null)
			{
				parameterType = nullableType;
			}
			if (value is long)
			{
				bool integer = parameterType == typeof(short)
					|| parameterType == typeof(int);
				if (integer)
				{
					return true;
				}
				TypeInfo typeInfo = parameterType.GetTypeInfo();
				if (typeInfo.IsEnum)
				{
					try
					{
						return Enum.IsDefined(parameterType, (int)(long)value);
					}
					catch (Exception)
					{
						Type enumType = Enum.GetUnderlyingType(parameterType);
						//Check if the enum is long or short instead of int
						if (enumType == typeof(long))
						{
							return Enum.IsDefined(parameterType, value);
						}
						else if (enumType == typeof(short))
						{
							return Enum.IsDefined(parameterType, (short)(long)value);
						}
					}
				}
				return false;
			}
			if (value is double || value is decimal)
			{
				return parameterType == typeof(double)
					|| parameterType == typeof(decimal)
					|| parameterType == typeof(float);
			}
			if (value is string)
			{
				if (parameterType == typeof(Guid))
				{
					return Guid.TryParse((string)value, out Guid guid);
				}
				if (parameterType.GetTypeInfo().IsEnum)
				{
					return Enum.IsDefined(parameterType, value);
				}
			}
			try
			{
				//TODO should just assume they will work and have the end just fail if cant convert?
				JsonSerializer serializer = JsonSerializer.Create(this.jsonSerializerSettings);
				if (value is JObject)
				{
					JObject jObject = (JObject)value;
					jObject.ToObject(parameterType, serializer); //Test conversion
					return true;
				}
				if (value is JArray)
				{
					JArray jArray = (JArray)value;
					jArray.ToObject(parameterType, serializer); //Test conversion
					return true;
				}
				//Final check to see if the conversion can happen
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				Convert.ChangeType(value, parameterType);
			}
			catch (Exception ex)
			{
				this.logger?.LogWarning($"Parameter '{parameterInfo.Name}' failed to deserialize: " + ex);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Detects if the request parameters match the method parameters and converts the map into an ordered list
		/// </summary>
		/// <param name="parametersMap">Map of parameter name to parameter value</param>
		/// <param name="parameterList">Result of converting the map to an ordered list, null if result is False</param>
		/// <returns>True if the request parameters match the method parameters, otherwise Fasle</returns>
		public bool HasParameterSignature(Dictionary<string, object> parametersMap, out object[] parameterList)
		{
			if (parametersMap == null)
			{
				throw new ArgumentNullException(nameof(parametersMap));
			}
			bool canParse = this.TryParseParameterList(parametersMap, out parameterList);
			if (!canParse)
			{
				return false;
			}
			bool hasSignature = this.HasParameterSignature(parameterList, out parameterList);
			if (hasSignature)
			{
				return true;
			}
			parameterList = null;
			return false;
		}


		/// <summary>
		/// Tries to parse the parameter map into an ordered parameter list
		/// </summary>
		/// <param name="parametersMap">Map of parameter name to parameter value</param>
		/// <param name="parameterList">Result of converting the map to an ordered list, null if result is False</param>
		/// <returns>True if the parameters can convert to an ordered list based on the method signature, otherwise Fasle</returns>
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

		/// <summary>
		/// Gets the parameter information list for the method
		/// </summary>
		/// <returns>Parameter info list for the method</returns>
		public IReadOnlyList<ParameterInfo> GetParameterList()
		{
			return this.parameterInfoList;
		}
	}
}
