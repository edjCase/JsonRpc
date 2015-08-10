using Microsoft.AspNet.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public class RpcInvoker
	{
		private List<RpcSection> Sections { get; }
		public RpcInvoker(List<RpcSection> sections)
		{
			this.Sections = sections;
		}

		internal RpcResponseBase InvokeRequest(RouteContext context, RpcRequest request, string section)
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
				//Only give a response if there is an id
				return rpcResponse;
			}
			return null;
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
			foreach (RpcSection rpcSection in this.Sections)
			{
				foreach (Type type in rpcSection.Types)
				{
					MethodInfo[] publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					foreach (MethodInfo publicMethod in publicMethods)
					{
						RpcMethod rpcMethod = new RpcMethod(type, rpcSection.Name, publicMethod);
						rpcMethods.Add(rpcMethod);
					}
				}
			}
			return rpcMethods;
		}

	}
}
