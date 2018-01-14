using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Options;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.AspNetCore.Http;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Router for Asp.Net to direct Http Rpc requests to the correct method, invoke it and return the proper response
	/// </summary>
	public class RpcRouter : IRouter
	{
		/// <summary>
		/// Component that logs actions from the router
		/// </summary>
		private ILogger<RpcRouter> logger { get; }
		/// <summary>
		/// Component that compresses Rpc responses
		/// </summary>
		private IRpcCompressor compressor { get; }

		/// <summary>
		/// Provider that allows the retrieval of all configured routes
		/// </summary>
		private IRpcRouteProvider routeProvider { get; }

		private IRpcRequestHandler routeHandler { get; }

		/// <param name="compressor">Component that compresses Rpc responses</param>
		/// <param name="logger">Component that logs actions from the router</param>
		/// <param name="routeProvider">Provider that allows the retrieval of all configured routes</param>
		public RpcRouter(ILogger<RpcRouter> logger, IRpcCompressor compressor, IRpcRouteProvider routeProvider, IRpcRequestHandler routeHandler)
		{
			this.logger = logger;
			this.compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
			this.routeProvider = routeProvider ?? throw new ArgumentNullException(nameof(routeProvider));
			this.routeHandler = routeHandler ?? throw new ArgumentNullException(nameof(routeHandler));
		}

		/// <summary>
		/// Generates the virtual path data for the router
		/// </summary>
		/// <param name="context">Virtual path context</param>
		/// <returns>Virtual path data for the router</returns>
		public VirtualPathData GetVirtualPath(VirtualPathContext context)
		{
			// We return null here because we're not responsible for generating the url, the route is.
			return null;
		}

		/// <summary>
		/// Takes a route/http contexts and attempts to parse, invoke, respond to an Rpc request
		/// </summary>
		/// <param name="context">Route context</param>
		/// <returns>Task for async routing</returns>
		public async Task RouteAsync(RouteContext context)
		{
			try
			{
				RpcPath requestPath;
				if (!context.HttpContext.Request.Path.HasValue)
				{
					requestPath = RpcPath.Default;
				}
				else
				{
					requestPath = RpcPath.Parse(context.HttpContext.Request.Path.Value);
				}
				if (!requestPath.TryRemoveBasePath(this.routeProvider.BaseRequestPath, out requestPath))
				{
					this.logger?.LogTrace("Request did not match the base request path. Skipping rpc router.");
					return;
				}
				HashSet<RpcPath> availableRoutes = this.routeProvider.GetRoutes();
				if (!availableRoutes.Any())
				{
					this.logger?.LogDebug($"Request matched base request path but no routes.");
					return;
				}
				this.logger?.LogInformation($"Rpc request route '{requestPath}' matched.");

				Stream contentStream = context.HttpContext.Request.Body;

				string jsonString;
				if (contentStream == null)
				{
					jsonString = null;
				}
				else
				{
					StreamReader streamReader = new StreamReader(contentStream);
					jsonString = streamReader.ReadToEnd().Trim();
					
				}

				var routeContext = DefaultRouteContext.FromHttpContext(context.HttpContext, this.routeProvider);
				string responseJson = await this.routeHandler.HandleRequestAsync(requestPath, jsonString, routeContext);

				if(responseJson == null)
				{
					//No response required
					return;
				}

				context.HttpContext.Response.ContentType = "application/json";

				bool responseSet = false;
				string acceptEncoding = context.HttpContext.Request.Headers["Accept-Encoding"];
				if (!string.IsNullOrWhiteSpace(acceptEncoding))
				{
					string[] encodings = acceptEncoding.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string encoding in encodings)
					{
						bool haveType = Enum.TryParse(encoding, true, out CompressionType compressionType);
						if (!haveType)
						{
							continue;
						}
						context.HttpContext.Response.Headers.Add("Content-Encoding", new[] { encoding });
						this.compressor.CompressText(context.HttpContext.Response.Body, responseJson, Encoding.UTF8, compressionType);
						responseSet = true;
						break;
					}
				}
				if (!responseSet)
				{
					await context.HttpContext.Response.WriteAsync(responseJson);
				}

				context.MarkAsHandled();

				this.logger?.LogInformation("Rpc request complete");
			}
			catch (Exception ex)
			{
				string errorMessage = "Unknown exception occurred when trying to process Rpc request. Marking route unhandled";
				this.logger?.LogException(ex, errorMessage);
				context.MarkAsHandled();
			}
		}
	}
}