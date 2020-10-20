using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common.Utilities;
using EdjCase.JsonRpc.Router.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EdjCase.JsonRpc.Router
{
	internal class DefaultRpcMethodInfo : IRpcMethodInfo
	{
		private readonly MethodInfo methodInfo;
		private readonly ObjectFactory objectFactory;
		public string Name => this.methodInfo.Name;
		public MethodInfo SourceMethodInfo => this.methodInfo;

		public IReadOnlyList<IRpcParameterInfo> Parameters { get; }

		public bool AllowAnonymous { get; }

		public IReadOnlyList<IAuthorizeData> AuthorizeDataList { get; }

		public Type RawReturnType => this.methodInfo.ReturnType;

		private DefaultRpcMethodInfo(
			MethodInfo methodInfo,
			IReadOnlyList<RpcParameterInfo> parameters,
			ObjectFactory objectFactory,
			bool allowAnonymous,
			IReadOnlyList<IAuthorizeData> authorizeDataList)
		{
			this.methodInfo = methodInfo;
			this.Parameters = parameters;
			this.objectFactory = objectFactory;
			this.AllowAnonymous = allowAnonymous;
			this.AuthorizeDataList = authorizeDataList;
		}

		public static DefaultRpcMethodInfo FromMethodInfo(MethodInfo methodInfo)
		{
			RpcParameterInfo[] parameters = methodInfo.GetParameters()
				.Select(RpcParameterInfo.FromParameter)
				.ToArray();
			(List<IAuthorizeData> authorizeDataList, bool allowAnonymous) = GetAttributeInfo(methodInfo.GetCustomAttributes());
			if (methodInfo.DeclaringType != null)
			{
				(List<IAuthorizeData> typeAuthorizeDataList, bool typeAllowAnonymous) = GetAttributeInfo(methodInfo.DeclaringType.GetCustomAttributes());
				allowAnonymous = allowAnonymous || typeAllowAnonymous;
				authorizeDataList.AddRange(typeAuthorizeDataList);
			}
			ObjectFactory objectFactory = ActivatorUtilities.CreateFactory(methodInfo.DeclaringType, Array.Empty<Type>());
			return new DefaultRpcMethodInfo(methodInfo, parameters, objectFactory, allowAnonymous, authorizeDataList);
		}

		private static (List<IAuthorizeData> Data, bool allowAnonymous) GetAttributeInfo(IEnumerable<Attribute> attributes)
		{
			bool allowAnonymous = false;
			var dataList = new List<IAuthorizeData>(10);
			foreach (Attribute attribute in attributes)
			{
				if (attribute is IAuthorizeData data)
				{
					dataList.Add(data);
				}
				if (!allowAnonymous && attribute is IAllowAnonymous)
				{
					allowAnonymous = true;
				}
			}
			return (dataList, allowAnonymous);
		}

		public object? Invoke(object[] parameters, IServiceProvider serviceProvider)
		{
			//Use service provider to create instance
			object? obj = this.objectFactory(serviceProvider, null);
			if (obj == null)
			{
				//Use reflection to create instance if service provider failed or is null
				obj = Activator.CreateInstance(this.methodInfo.DeclaringType!);
			}
			return this.methodInfo.Invoke(obj, parameters);
		}
	}

	internal class RpcParameterInfo : IRpcParameterInfo
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

		public static RpcParameterInfo FromParameter(ParameterInfo parameterInfo)
		{
			Type parameterType = parameterInfo.ParameterType;
			RpcParameterType type = RpcParameterUtil.GetRpcType(parameterType);
			return new RpcParameterInfo(parameterInfo.Name!, type, parameterInfo.ParameterType, parameterInfo.IsOptional);
		}
	}
}
