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
using EdjCase.JsonRpc.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Threading;
using EdjCase.JsonRpc.Router.RouteProviders;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Router for Asp.Net to direct Web Socket Rpc requests to the correct method, invoke it and return the proper response
	/// </summary>
	public class RpcWebSocketRouter : IRouter
	{
		/// <summary>
		/// Provider that allows the retrieval of all configured routes
		/// </summary>
		private IRpcRouteProvider routeProvider { get; }

		/// <param name="methodProvider">Provider that allows the retrieval of all configured methods</param>
		public RpcWebSocketRouter(IRpcRouteProvider routeProvider)
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
			if (!context.HttpContext.WebSockets.IsWebSocketRequest)
			{
				return;
			}
			ILogger<RpcWebSocketRouter> logger = context.HttpContext.RequestServices.GetService<ILogger<RpcWebSocketRouter>>();
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
				logger?.LogInformation($"Rpc web socket request is attempting to open.");

				var scopeFactory = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
				using (WebSocket socket = await context.HttpContext.WebSockets.AcceptWebSocketAsync())
				{
					while (socket.State == WebSocketState.Open)
					{
						var buffer = new ArraySegment<byte>(new byte[1_000_000]);
						WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
						while (!result.EndOfMessage)
						{
							result = await socket.ReceiveAsync(buffer, CancellationToken.None);
						}

						switch (result.MessageType)
						{
							case WebSocketMessageType.Binary:
								throw new NotImplementedException();
							case WebSocketMessageType.Text:
								using (IServiceScope scope = scopeFactory.CreateScope())
								{
									string jsonString = Encoding.UTF8.GetString(buffer.ToArray());

									var requestHandler = scope.ServiceProvider.GetRequiredService<IRpcRequestHandler>();
									var routeContext = new DefaultRouteContext(scope.ServiceProvider, context.HttpContext.User, this.routeProvider);
									string responseJson = await requestHandler.HandleRequestAsync(RpcPath.Default, jsonString, routeContext);
									if (!string.IsNullOrWhiteSpace(responseJson))
									{
										byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
										await socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
									}
								}
								break;
							case WebSocketMessageType.Close:
								logger?.LogInformation($"Rpc web socket request is closing by request of the client.");
								return;
						}
					}
				}

				context.MarkAsHandled();

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