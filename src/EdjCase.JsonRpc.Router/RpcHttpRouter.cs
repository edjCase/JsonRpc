﻿using System;
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
using EdjCase.JsonRpc.Core.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Router for Asp.Net to direct Http Rpc requests to the correct method, invoke it and return the proper response
	/// </summary>
	public class RpcHttpRouter : IRouter
	{
		/// <summary>
		/// Provider that allows the retrieval of all configured routes
		/// </summary>
		private IRpcRouteProvider routeProvider { get; }

		/// <param name="routeProvider">Provider that allows the retrieval of all configured routes</param>
		public RpcHttpRouter(IRpcRouteProvider routeProvider)
		{
			this.routeProvider = routeProvider ?? throw new ArgumentNullException(nameof(routeProvider));
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
			ILogger<RpcHttpRouter> logger = context.HttpContext.RequestServices.GetService<ILogger<RpcHttpRouter>>();
			try
			{
				RpcPath requestPath;
				if (!context.HttpContext.Request.Path.HasValue)
				{
					requestPath = RpcPath.Default;
				}
				else
				{
					if (!RpcPath.TryParse(context.HttpContext.Request.Path.Value, out requestPath))
					{
						logger?.LogInformation($"Could not parse the path '{context.HttpContext.Request.Path.Value}' for the " +
							$"request into an rpc path. Skipping rpc router middleware.");
						return;
					}
				}
				if (!requestPath.TryRemoveBasePath(this.routeProvider.BaseRequestPath, out requestPath))
				{
					logger?.LogTrace("Request did not match the base request path. Skipping rpc router.");
					return;
				}
				logger?.LogInformation($"Rpc request with route '{requestPath}' started.");

				string jsonString;
				if (context.HttpContext.Request.Body == null)
				{
					jsonString = null;
				}
				else
				{
					using (StreamReader streamReader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8,
						detectEncodingFromByteOrderMarks: true,
						bufferSize: 1024,
						leaveOpen: true))
					{
						try
						{
							jsonString = await streamReader.ReadToEndAsync();
						}
						catch (TaskCanceledException ex)
						{
							throw new RpcCanceledRequestException("Cancelled while reading the request.", ex);
						}
						jsonString = jsonString.Trim();
					}

				}

				IRpcRequestHandler requestHandler = context.HttpContext.RequestServices.GetRequiredService<IRpcRequestHandler>();
				var routeContext = DefaultRouteContext.FromHttpContext(context.HttpContext, this.routeProvider);
				string responseJson = await requestHandler.HandleRequestAsync(requestPath, jsonString, routeContext);

				if (responseJson == null)
				{
					//No response required, but status code must be 204
					context.HttpContext.Response.StatusCode = 204;
					context.MarkAsHandled();
					return;
				}

				context.HttpContext.Response.ContentType = "application/json";

				bool responseSet = false;
				string acceptEncoding = context.HttpContext.Request.Headers["Accept-Encoding"];
				if (!string.IsNullOrWhiteSpace(acceptEncoding))
				{
					IStreamCompressor compressor = context.HttpContext.RequestServices.GetService<IStreamCompressor>();
					if (compressor != null)
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
							using (Stream responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson)))
							{
								compressor.Compress(responseStream, context.HttpContext.Response.Body, compressionType);
							}
							responseSet = true;
							break;
						}
					}
				}
				if (!responseSet)
				{
					await context.HttpContext.Response.WriteAsync(responseJson);
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
	}
}