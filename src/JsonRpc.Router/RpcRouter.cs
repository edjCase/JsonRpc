using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;

namespace JsonRpc.Router
{
	public class RpcRouter : IRouter
	{
		private RpcRouterConfiguration Configuration { get; }
		public RpcRouter(RpcRouterConfiguration configuration, IRpcInvoker invoker = null)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			if(invoker == null)
			{
				invoker = new DefaultRpcInvoker(configuration.Sections);
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
			List<RpcRequest> requests;
			bool isRpc = this.TryGetRpcRequests(context, out requests);
			if (!isRpc)
			{
				return;
			}

			var invokingTasks = new List<Task<RpcResponseBase>>();
			IRpcInvoker invoker = new DefaultRpcInvoker(this.Configuration.Sections);
			foreach (RpcRequest request in requests)
			{
				Task<RpcResponseBase> invokingTask = Task.Run(() => invoker.InvokeRequest(request, section));
				invokingTasks.Add(invokingTask);
			}
			await Task.WhenAll(invokingTasks.ToArray());

			List<RpcResponseBase> responses = invokingTasks
				.Select(t => t.Result)
				.Where(r => r != null)
				.ToList();

			await this.SetResponse(context, responses);
			context.IsHandled = true;
		}

		private bool TryGetRpcRequests(RouteContext context, out List<RpcRequest> rpcRequests)
		{
			rpcRequests = null;

			Stream contentStream = context.HttpContext.Request.Body;

			if (contentStream == null)
			{
				return false;
			}
			using (StreamReader streamReader = new StreamReader(contentStream))
			{
				string jsonString = streamReader.ReadToEnd().Trim();
				if (string.IsNullOrWhiteSpace(jsonString))
				{
					return false;
				}
				try
				{
					if (!this.IsSingleRequest(jsonString))
					{
						rpcRequests = JsonConvert.DeserializeObject<List<RpcRequest>>(jsonString);
					}
					else
					{
						rpcRequests = new List<RpcRequest>();
						RpcRequest rpcRequest = JsonConvert.DeserializeObject<RpcRequest>(jsonString);
						if (rpcRequest != null)
						{
							rpcRequests.Add(rpcRequest);
						}
					}

					if (!rpcRequests.Any())
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

		private bool IsSingleRequest(string jsonString)
		{
			if (jsonString == null || jsonString.Length < 1)
			{
				throw new ArgumentNullException(nameof(jsonString));
			}
			for(int i = 0; i < jsonString.Length; i++)
			{
				char character = jsonString[i];
				switch (character)
				{
					case '{':
						return true;
					case '[':
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


		private async Task SetResponse(RouteContext context, List<RpcResponseBase> responses)
		{
			if (responses == null || !responses.Any())
			{
				return;
			}
			Stream responseStream = context.HttpContext.Response.Body;
			using (StreamWriter streamWriter = new StreamWriter(responseStream))
			{
				string resultJson;
				if (responses.Count == 1)
				{
					resultJson = JsonConvert.SerializeObject(responses.First());
				}
				else
				{
					resultJson = JsonConvert.SerializeObject(responses);
				}
				await streamWriter.WriteAsync(resultJson);
			}
		}
	}
}