using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcMethodProvider
	{
		RpcRouteMetaData Get();
	}


	public class RpcRouteMetaData
	{
		public IReadOnlyList<IRpcMethodInfo> BaseRoute { get; }
		public IReadOnlyDictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>> PathRoutes { get; }

		public RpcRouteMetaData(IReadOnlyList<IRpcMethodInfo> baseMethods, IReadOnlyDictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>> methods)
		{
			this.BaseRoute = baseMethods;
			this.PathRoutes = methods;
		}
	}

	public interface IRpcMethodInfo
	{
		string Name { get; }
		IReadOnlyList<IRpcParameterInfo> Parameters { get; }
		bool AllowAnonymous { get; }
		IReadOnlyList<IAuthorizeData> AuthorizeDataList { get; }
		Type RawReturnType { get; }

		object? Invoke(object[] parameters, IServiceProvider serviceProvider);
	}

	public interface IRpcParameterInfo
	{
		string Name { get; }
		Type RawType { get; }
		bool IsOptional { get; }
	}

	public static class MethodProviderExtensions
	{
		public static IReadOnlyList<IRpcMethodInfo>? GetByPath(this IRpcMethodProvider methodProvider, RpcPath? path)
		{
			RpcRouteMetaData metaData = methodProvider.Get();
			if (path == null)
			{
				return metaData.BaseRoute;
			}
			if (metaData.PathRoutes.TryGetValue(path, out IReadOnlyList<IRpcMethodInfo> methods))
			{
				return methods;
			}
			return null;
		}
	}
}

