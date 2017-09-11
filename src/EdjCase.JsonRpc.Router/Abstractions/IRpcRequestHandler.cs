using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcRequestHandler
	{
		Task<string> HandleRequestAsync(RpcPath requestPath, string requestBody, IRouteContext routeContext);
	}
}
