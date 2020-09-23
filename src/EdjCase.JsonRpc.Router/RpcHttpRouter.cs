using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Options;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.AspNetCore.Http;
using EdjCase.JsonRpc.Common.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Reflection;
using System.IO.Compression;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Router for Asp.Net to direct Http Rpc requests to the correct method, invoke it and return the proper response
	/// </summary>
	internal class RpcHttpRouter : IRouter
	{
		private static readonly char[] encodingSeperators = new[] { ',', ' ' };

		/// <summary>
		/// Generates the virtual path data for the router
		/// </summary>
		/// <param name="context">Virtual path context</param>
		/// <returns>Virtual path data for the router</returns>
		public VirtualPathData? GetVirtualPath(VirtualPathContext context)
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
			ILogger<RpcHttpRouter> logger = context.HttpContext.RequestServices.GetService<ILogger<RpcHttpRouter>>();
			try
			{
				RpcPath? requestPath;
				if (!context.HttpContext.Request.Path.HasValue)
				{
					requestPath = null;
				}
				else
				{
					if (!RpcPath.TryParse(context.HttpContext.Request.Path.Value.AsSpan(), out requestPath))
					{
						logger?.LogInformation($"Could not parse the path '{context.HttpContext.Request.Path.Value}' for the " +
							$"request into an rpc path. Skipping rpc router middleware.");
						return;
					}
				}
				logger?.LogInformation($"Rpc request with route '{requestPath}' started.");


				IRpcRequestHandler requestHandler = context.HttpContext.RequestServices.GetRequiredService<IRpcRequestHandler>();
				var routeContext = new RpcContext(context.HttpContext.RequestServices, requestPath);
				context.HttpContext.RequestServices.GetRequiredService<IRpcContextAccessor>().Set(routeContext);
				Stream writableStream = this.BuildWritableResponseStream(context.HttpContext);
				using (var requestBody = new MemoryStream())
				{
					await context.HttpContext.Request.Body.CopyToAsync(requestBody);
					requestBody.Position = 0;
					bool hasResponse = await requestHandler.HandleRequestAsync(requestBody, writableStream);
					if (!hasResponse)
					{
						//No response required, but status code must be 204
						context.HttpContext.Response.StatusCode = 204;
						context.MarkAsHandled();
						return;
					}
				}


				context.MarkAsHandled();

				logger?.LogInformation("Rpc request complete");
			}
			catch (Exception ex)
			{
				string errorMessage = "Unknown exception occurred when trying to process Rpc request. Marking route unhandled";
				logger?.LogException(ex, errorMessage);
				context.MarkAsHandled();
			}
		}

		private Stream BuildWritableResponseStream(HttpContext httpContext)
		{
			httpContext.Response.ContentType = "application/json";
			string acceptEncoding = httpContext.Request.Headers["Accept-Encoding"];
			if (!string.IsNullOrWhiteSpace(acceptEncoding))
			{
				IStreamCompressor compressor = httpContext.RequestServices.GetService<IStreamCompressor>();
				if (compressor != null)
				{
					string[] encodings = acceptEncoding.Split(RpcHttpRouter.encodingSeperators, StringSplitOptions.RemoveEmptyEntries);
					foreach (string encoding in encodings)
					{
						if (compressor.TryGetCompressionStream(httpContext.Response.Body, encoding, CompressionMode.Compress, out Stream compressedStream))
						{
							httpContext.Response.Headers.Add("Content-Encoding", encoding);
							return compressedStream;
						}
					}
				}
			}
			return httpContext.Response.Body;
		}
	}
}