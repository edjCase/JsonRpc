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
			string groupName;
			if (!this.IsCorrectRoute(context, out groupName))
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

				await InvokeRequest(context, request, groupName);
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

		private bool IsCorrectRoute(RouteContext context, out string groupName)
		{
			PathString remainingPath;
			bool isRpcRoute = context.HttpContext.Request.Path.StartsWithSegments(this.Configuration.RoutePrefix, out remainingPath);
			if (!isRpcRoute)
			{
				groupName = null;
				return false;
			}
			string[] pathComponents = remainingPath.Value?.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (pathComponents == null || !pathComponents.Any())
			{
				groupName = null;
				return false;
			}
			groupName = pathComponents.First();
			if (string.IsNullOrWhiteSpace(groupName))
			{
				groupName = null;
				return false;
			}
			return true;
		}

		private async Task InvokeRequest(RouteContext context, RpcRequest request, string groupName)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			if (!string.Equals(request.JsonRpc, "2.0"))
			{
				throw new InvalidRpcRequestException("Request must be jsonrpc version '2.0'");
			}
			try
			{
				object[] parameterList;
				RpcMethod rpcMethod = this.GetMatchingMethod(groupName, request, out parameterList);

				object result = rpcMethod.Invoke(parameterList);

				RpcResultResponse rpcResponse = new RpcResultResponse(request.Id, result);
				await this.SetResponse(context, rpcResponse);
			}
			catch (RpcException ex)
			{
				RpcError error = new RpcError(ex);
				RpcErrorResponse rpcErrorResponse = new RpcErrorResponse(request.Id, error);
				await this.SetResponse(context, rpcErrorResponse);
				return;
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
				RpcErrorResponse rpcErrorResponse = new RpcErrorResponse(request.Id, error);
				await this.SetResponse(context, rpcErrorResponse);
				return;
			}
		}
		private RpcMethod GetMatchingMethod(string groupName, RpcRequest request, out object[] parameterList)
		{
			if (string.IsNullOrWhiteSpace(groupName))
			{
				throw new ArgumentNullException(nameof(groupName));
			}
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			List<RpcMethod> methods = this.GetRpcMethods();

			methods = methods
				.Where(m => string.Equals(m.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
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

	public class RpcRouterConfiguration
	{
		internal Dictionary<string, List<Type>> Sections { get; set; } = new Dictionary<string, List<Type>>();
		public PathString RoutePrefix { get; set; }

		public void RegisterClassToRpcSection<T>(string sectionName = null)
		{
			Type type = typeof(T);
			if (this.Sections.ContainsKey(sectionName) && this.Sections[sectionName].Any(t => t == type))
			{
				throw new ArgumentException($"Type '{type.FullName}' has already been registered with the Rpc router under the section '{sectionName}'");
			}
			List<Type> typeList;
			if(!this.Sections.TryGetValue(sectionName, out typeList))
			{
				typeList = new List<Type>();
				this.Sections[sectionName] = typeList;
			}
			typeList.Add(type);
		}
	}
}