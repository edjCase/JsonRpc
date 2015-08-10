using JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JsonRpc.Router
{
	public class DefaultRpcInvoker : IRpcInvoker
	{
		private List<RpcSection> Sections { get; }
		public DefaultRpcInvoker(List<RpcSection> sections)
		{
			if (sections == null)
			{
				throw new ArgumentNullException(nameof(sections));
			}
			if (!sections.Any())
			{
				throw new ArgumentException("There must be at least on section defined", nameof(sections));
			}
			this.Sections = sections;
		}

		public List<RpcResponseBase> InvokeBatchRequest(List<RpcRequest> requests, string section)
		{
			var invokingTasks = new List<Task<RpcResponseBase>>();
			foreach (RpcRequest request in requests)
			{
				Task<RpcResponseBase> invokingTask = Task.Run(() => this.InvokeRequest(request, section));
				invokingTasks.Add(invokingTask);
			}

			Task.WaitAll(invokingTasks.ToArray());

			List<RpcResponseBase> responses = invokingTasks
				.Select(t => t.Result)
				.Where(r => r != null)
				.ToList();

			return responses;
		}

		public RpcResponseBase InvokeRequest(RpcRequest request, string section)
		{
			RpcResponseBase rpcResponse;
			try
			{
				if (request == null)
				{
					throw new ArgumentNullException(nameof(request));
				}
				if (!string.Equals(request.JsonRpcVersion, "2.0"))
				{
					throw new InvalidRpcRequestException("Request must be jsonrpc version '2.0'");
				}

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
