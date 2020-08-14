using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcMethodProvider
	{
		IReadOnlyList<IRpcMethodInfo> GetByPath(RpcPath? path = null);
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
}

