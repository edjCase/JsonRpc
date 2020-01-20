using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcAuthorizationHandler
	{
		Task<bool> IsAuthorizedAsync(RpcMethodInfo methodInfo);
	}
}
