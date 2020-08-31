using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcMethodProvider
	{
		IRpcRouteMetaData Get();
	}

	public interface IRpcRouteMetaData
	{
		IReadOnlyList<IRpcMethodInfo> BaseRoute { get; }
		IReadOnlyDictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>> PathRoutes { get; }
	}

	public interface IRpcMethodInfo
	{
		string Name { get; }
		IReadOnlyList<IRpcParameterInfo> Parameters { get; }
		bool AllowAnonymous { get; }
		IReadOnlyList<IAuthorizeData> AuthorizeDataList { get; }

		object? Invoke(object[] parameters, IServiceProvider serviceProvider);
	}

	public interface IRpcParameterInfo
	{
		string Name { get; }
		RpcParameterType Type { get; }
		Type RawType { get; }
		bool IsOptional { get; }
	}

	public static class MethodProviderExtensions
	{
		public static IReadOnlyList<IRpcMethodInfo>? GetByPath(this IRpcMethodProvider methodProvider, RpcPath? path)
		{
			IRpcRouteMetaData metaData = methodProvider.Get();
			if(path == null)
			{
				return metaData.BaseRoute;
			}
			if(metaData.PathRoutes.TryGetValue(path, out IReadOnlyList<IRpcMethodInfo> methods))
			{
				return methods;
			}
			return null;
		}
	}
}

