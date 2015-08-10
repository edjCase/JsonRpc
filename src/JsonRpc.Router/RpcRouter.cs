using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using JsonRpc.Router.Abstractions;

namespace JsonRpc.Router
{
	public class RpcRouter : IRouter
	{
		private RpcRouterConfiguration Configuration { get; }
		private IRpcInvoker Invoker { get; }
		private IRpcParser Parser { get; }
		public RpcRouter(RpcRouterConfiguration configuration, IRpcInvoker invoker = null, IRpcParser parser = null) //TODO better DI
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			if (invoker == null)
			{
				this.Invoker = new DefaultRpcInvoker(configuration.Sections);
			}
			if (parser == null)
			{
				this.Parser = new DefaultRpcParser(configuration.RoutePrefix, configuration.Sections);
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
			try
			{
				string section;
				bool matchesRoute = this.Parser.MatchesRpcRoute(context.HttpContext.Request.Path, out section);
				if (!matchesRoute)
				{
					return;
				}

				try
				{
					Stream contentStream = context.HttpContext.Request.Body;

					string jsonString;
					if (contentStream == null)
					{
						jsonString = null;
					}
					else
					{
						using (StreamReader streamReader = new StreamReader(contentStream))
						{
							jsonString = streamReader.ReadToEnd().Trim();
						}
					}
					List<RpcRequest> requests = this.Parser.ParseRequests(jsonString);

					List<RpcResponseBase> responses = this.Invoker.InvokeBatchRequest(requests, section);

					await this.SetResponse(context, responses);
					context.IsHandled = true;
				}
				catch (RpcException ex)
				{
					context.IsHandled = true;
					await this.SetErrorResponse(context, ex);
					return;
				}
			}
			catch (Exception)
			{
				context.IsHandled = false;
			}
		}

		private async Task SetErrorResponse(RouteContext context, RpcException exception)
		{
			var responses = new List<RpcResponseBase>
			{
				new RpcErrorResponse(null, new RpcError(exception))
			};
			await this.SetResponse(context, responses);
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