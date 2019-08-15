using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcRequestHandler
	{
		Task HandleRequestAsync(RpcPath requestPath, Stream requestBody, IRouteContext routeContext, Stream responseBody);
	}

	public static class RpcRequestHandlerExtensions
	{
		public static async Task<string> HandleRequestAsync(this IRpcRequestHandler handler, RpcPath requestPath, string requestJson, IRouteContext routeContext)
		{
			using (var requestStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)))
			{
				using (var responseStream = new MemoryStream())
				{
					await handler.HandleRequestAsync(requestPath, requestStream, routeContext, responseStream);
					responseStream.Position = 0;
					return await new StreamReader(responseStream).ReadToEndAsync();
				}
			}
		}
	}
}
