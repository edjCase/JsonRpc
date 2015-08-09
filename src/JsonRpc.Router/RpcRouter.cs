using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.AspNet.Http;

namespace JsonRpc.Router
{
	internal class RpcRouter : IRouter
	{
		private RpcRouterConfiguration Configuration { get; }
		public RpcRouter(RpcRouterConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			this.Configuration = configuration;
		}

		public VirtualPathData GetVirtualPath(VirtualPathContext context)
		{
			// We return null here because we're not responsible for generating the url, the route is.
			return null;
		}

		public async Task RouteAsync(RouteContext context)
		{
			string section;
			if (!this.IsCorrectRoute(context, out section))
			{
				return;
			}
			RpcRequest request;
			bool isRpc = this.TryGetRpcRequest(context, out request);
			if (!isRpc)
			{
				return;
			}

			// Replacing the route data allows any code running here to dirty the route values or data-tokens
			// without affecting something upstream.
			RouteData oldRouteData = context.RouteData;
			RouteData newRouteData = new RouteData(oldRouteData);

			try
			{
				context.RouteData = newRouteData;

				await InvokeRequest(context, request, section);
				context.IsHandled = true;
			}
			finally
			{
				if (!context.IsHandled)
				{
					context.RouteData = oldRouteData;
				}
			}
		}

		private bool TryGetRpcRequest(RouteContext context, out RpcRequest rpcRequest)
		{
			rpcRequest = null;

			Stream contentStream = context.HttpContext.Request.Body;

			if (contentStream == null)
			{
				return false;
			}
			using (StreamReader streamReader = new StreamReader(contentStream))
			{
				string jsonString = streamReader.ReadToEnd();
				if (string.IsNullOrWhiteSpace(jsonString))
				{
					return false;
				}
				try
				{
					rpcRequest = JsonConvert.DeserializeObject<RpcRequest>(jsonString);
					if (rpcRequest == null)
					{
						return false;
					}
				}
				catch (Exception)
				{
					return false;
				}
			}
			return true;
		}

		private bool IsCorrectRoute(RouteContext context, out string section)
		{
			PathString remainingPath;
			bool isRpcRoute = context.HttpContext.Request.Path.StartsWithSegments(this.Configuration.RoutePrefix, out remainingPath);
			if (!isRpcRoute)
			{
				section = null;
				return false;
			}
			string[] pathComponents = remainingPath.Value?.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (pathComponents == null || !pathComponents.Any())
			{
				section = null;
				return false;
			}
			section = pathComponents.First();
			if (string.IsNullOrWhiteSpace(section))
			{
				section = null;
				return false;
			}
			return true;
		}

		private async Task InvokeRequest(RouteContext context, RpcRequest request, string section)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			if (!string.Equals(request.JsonRpc, "2.0"))
			{
				throw new InvalidRpcRequestException("Request must be jsonrpc version '2.0'");
			}
			RpcResponseBase rpcResponse;
			try
			{
				object[] parameterList;
				RpcMethod rpcMethod = this.GetMatchingMethod(section, request, out parameterList);

				object result = rpcMethod.Invoke(parameterList);

				rpcResponse = new RpcResultResponse(request.Id, result);
			}
			catch (RpcException ex)
			{
				RpcError error = new RpcError(ex);
				rpcResponse = new RpcErrorResponse(request.Id, error);
			}
#if DEBUG
			catch (Exception ex)
			{
				string message = ex.Message;
#else
			catch (Exception)
			{
				string message = "An internal server error has occurred";
#endif
				int code = -32603;
				object data = null;
				RpcError error = new RpcError(code, message, data);
				rpcResponse = new RpcErrorResponse(request.Id, error);
			}
			if (request.Id != null)
			{
				//Only set a response if there is an id
				await this.SetResponse(context, rpcResponse);
			}
		}
		private RpcMethod GetMatchingMethod(string section, RpcRequest request, out object[] parameterList)
		{
			if (string.IsNullOrWhiteSpace(section))
			{
				throw new ArgumentNullException(nameof(section));
			}
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			List<RpcMethod> methods = this.GetRpcMethods();

			methods = methods
				.Where(m => string.Equals(m.Section, section, StringComparison.OrdinalIgnoreCase))
				.Where(m => string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase))
				.ToList();


			RpcMethod rpcMethod = null;
			parameterList = null;
			foreach (RpcMethod method in methods)
			{
				bool matchingMethod;
				if (request.ParameterMap != null)
				{
					matchingMethod = method.HasParameterSignature(request.ParameterMap, out parameterList);
				}
				else
				{
					matchingMethod = method.HasParameterSignature(request.ParameterList);
					parameterList = request.ParameterList;
				}
				if (matchingMethod)
				{
					if (rpcMethod != null) //If already found a match
					{
						throw new AmbiguousRpcMethodException();
					}
					rpcMethod = method;
				}
			}

			if (rpcMethod == null)
			{
				throw new RpcMethodNotFoundException();
			}
			return rpcMethod;
		}

		private List<RpcMethod> GetRpcMethods()
		{
			List<RpcMethod> rpcMethods = new List<RpcMethod>();
			foreach (var rpcSection in this.Configuration.Sections)
			{
				foreach (Type type in rpcSection.Value)
				{
					MethodInfo[] publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					foreach (MethodInfo publicMethod in publicMethods)
					{
						RpcMethod rpcMethod = new RpcMethod(type, rpcSection.Key, publicMethod);
						rpcMethods.Add(rpcMethod);
					}
				}
			}
			return rpcMethods;
		}

		private async Task SetResponse(RouteContext context, object response)
		{
			Stream responseStream = context.HttpContext.Response.Body;
			using (StreamWriter streamWriter = new StreamWriter(responseStream))
			{
				string resultJson = JsonConvert.SerializeObject(response);
				await streamWriter.WriteAsync(resultJson);
			}
		}
	}
}