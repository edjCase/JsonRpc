using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcMethodProvider
	{
		IReadOnlyList<MethodInfo> Get();
	}
}
