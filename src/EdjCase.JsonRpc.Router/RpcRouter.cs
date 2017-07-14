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

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Router for Asp.Net to direct Http Rpc requests to the correct method, invoke it and return the proper response
	/// </summary>
	public class RpcRouter : IRouter
	{
		/// <summary>
		/// Configuration data for the server
		/// </summary>
		private IOptions<RpcServerConfiguration> serverConfig { get; }
		/// <summary>
		/// Component that invokes Rpc requests target methods and returns a response
		/// </summary>
		private IRpcInvoker invoker { get; }
		/// <summary>
		/// Component that parses Http requests into Rpc requests
		/// </summary>
		private IRpcParser parser { get; }
		/// <summary>
		/// Component that compresses Rpc responses
		/// </summary>
		private IRpcCompressor compressor { get; }
		/// <summary>
		/// Component that logs actions from the router
		/// </summary>
		private ILogger<RpcRouter> logger { get; }

		/// <summary>
		/// Provider that allows the retrieval of all configured routes
		/// </summary>
		private IRpcRouteProvider routeProvider { get; }

		/// <param name="serverConfig">Configuration data for the server</param>
		/// <param name="invoker">Component that invokes Rpc requests target methods and returns a response</param>
		/// <param name="parser">Component that parses Http requests into Rpc requests</param>
		/// <param name="compressor">Component that compresses Rpc responses</param>
		/// <param name="logger">Component that logs actions from the router</param>
		/// <param name="routeProvider">Provider that allows the retrieval of all configured routes</param>
		public RpcRouter(IOptions<RpcServerConfiguration> serverConfig, IRpcInvoker invoker, IRpcParser parser, IRpcCompressor compressor, ILogger<RpcRouter> logger,
			IRpcRouteProvider routeProvider)
		{
			this.serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
			this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
			this.logger = logger;
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
				if (!requestPath.StartsWith(this.routeProvider.BaseRequestPath))
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
					List<RpcRequest> requests = this.parser.ParseRequests(jsonString, out bool isBulkRequest, this.serverConfig.Value.JsonSerializerSettings);
					this.logger?.LogInformation($"Processing {requests.Count} Rpc requests");

					int batchLimit = this.serverConfig.Value.BatchRequestLimit;
					List<RpcResponse> responses;
					if (batchLimit > 0 && requests.Count > batchLimit)
					{
						string batchLimitError = $"Request count exceeded batch request limit ({batchLimit}).";
						responses = new List<RpcResponse>
						{
							new RpcResponse(null, new RpcError(RpcErrorCode.InvalidRequest, batchLimitError))
						};
						this.logger?.LogError(batchLimitError + " Returning error response.");
					}
					else
					{
						IRouteContext routeContext = DefaultRouteContext.FromHttpContext(context.HttpContext, routeProvider);
						responses = await this.invoker.InvokeBatchRequestAsync(requests, requestPath, routeContext);
					}



					this.logger?.LogInformation($"Sending '{responses.Count}' Rpc responses");
					await this.SetResponse(context, responses, isBulkRequest, this.serverConfig.Value.JsonSerializerSettings);
					context.MarkAsHandled();

					this.logger?.LogInformation("Rpc request complete");
				}
				catch (RpcException ex)
				{
					context.MarkAsHandled();
					this.logger?.LogException(ex, "Error occurred when proccessing Rpc request. Sending Rpc error response");
					await this.SetErrorResponse(context, ex);
				}
			}
			catch (Exception ex)
			{
				string errorMessage = "Unknown exception occurred when trying to process Rpc request. Marking route unhandled";
				this.logger?.LogException(ex, errorMessage);
				context.MarkAsHandled();
			}
		}

		/// <summary>
		/// Sets the http response to the corresponding Rpc exception
		/// </summary>
		/// <param name="context">Route context</param>
		/// <param name="exception">Exception from Rpc request processing</param>
		/// <returns>Task for async call</returns>
		private async Task SetErrorResponse(RouteContext context, RpcException exception)
		{
			var responses = new List<RpcResponse>
			{
				new RpcResponse(null, new RpcError(exception, this.serverConfig.Value.ShowServerExceptions))
			};
			await this.SetResponse(context, responses, false, this.serverConfig.Value.JsonSerializerSettings);
		}

		/// <summary>
		/// Sets the http response with the given Rpc responses
		/// </summary>
		/// <param name="context">Route context</param>
		/// <param name="responses">Responses generated from the Rpc request(s)</param>
		/// <param name="isBulkRequest">True if the request should be sent back as an array or a single object</param>
		/// <param name="jsonSerializerSettings">Json serialization settings that will be used in serialization and deserialization for rpc requests</param>
		/// <returns>Task for async call</returns>
		private async Task SetResponse(RouteContext context, List<RpcResponse> responses, bool isBulkRequest, JsonSerializerSettings jsonSerializerSettings = null)
		{
			if (responses == null || !responses.Any())
			{
				return;
			}

			string resultJson = !isBulkRequest
				? JsonConvert.SerializeObject(responses.Single(), jsonSerializerSettings)
				: JsonConvert.SerializeObject(responses, jsonSerializerSettings);

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