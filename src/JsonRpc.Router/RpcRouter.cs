using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonRpc.Router.Abstractions;

namespace JsonRpc.Router
{
	public class RpcRouter : IRouter
	{
		private RpcRouterConfiguration configuration { get; }
		private IRpcInvoker invoker { get; }
		private IRpcParser parser { get; }
		private IRpcCompressor compressor { get; }
		public RpcRouter(RpcRouterConfiguration configuration, IRpcInvoker invoker, IRpcParser parser, IRpcCompressor compressor)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}
			if (invoker == null)
			{
				throw new ArgumentNullException(nameof(invoker));
			}
			if (parser == null)
			{
				throw new ArgumentNullException(nameof(parser));
			}
			if (compressor == null)
			{
				throw new ArgumentNullException(nameof(compressor));
			}
			this.configuration = configuration;
			this.invoker = invoker;
			this.parser = parser;
			this.compressor = compressor;
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
				RpcRoute route;
				bool matchesRoute = this.parser.MatchesRpcRoute(this.configuration.Routes, context.HttpContext.Request.Path, out route);
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
					List<RpcRequest> requests = this.parser.ParseRequests(jsonString);

					List<RpcResponseBase> responses = this.invoker.InvokeBatchRequest(requests, route);

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

			string resultJson = responses.Count == 1
				? JsonConvert.SerializeObject(responses.First())
				: JsonConvert.SerializeObject(responses);

			string acceptEncoding = context.HttpContext.Request.Headers["Accept-Encoding"];
			if (!string.IsNullOrWhiteSpace(acceptEncoding))
			{
				string[] encodings = acceptEncoding.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);
				foreach (string encoding in encodings)
				{
					CompressionType compressionType;
					bool haveType = Enum.TryParse(encoding, true, out compressionType);
					if (!haveType)
					{
						continue;
					}
					context.HttpContext.Response.Headers.Add("Content-Encoding", new[] {encoding});
					this.compressor.CompressText(context.HttpContext.Response.Body, resultJson, Encoding.UTF8, compressionType);
					return;
				}
			}

			Stream responseStream = context.HttpContext.Response.Body;
			using (StreamWriter streamWriter = new StreamWriter(responseStream))
			{
					
				await streamWriter.WriteAsync(resultJson);
			}
		}
	}
}