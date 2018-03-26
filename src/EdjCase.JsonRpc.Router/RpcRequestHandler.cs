using Edjcase.JsonRpc.Router;
using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	public class RpcRequestHandler : IRpcRequestHandler
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
		/// Component that logs actions from the handler
		/// </summary>
		private ILogger<RpcRequestHandler> logger { get; }
		/// <summary>
		/// Component that serializes all the responses to json
		/// </summary>
		private IRpcResponseSerializer responseSerializer { get; }

		/// <param name="serverConfig">Configuration data for the server</param>
		/// <param name="invoker">Component that invokes Rpc requests target methods and returns a response</param>
		/// <param name="parser">Component that parses Http requests into Rpc requests</param>
		/// <param name="responseSerializer">Component that serializes all the responses to json</param>
		/// <param name="logger">Component that logs actions from the router</param>
		public RpcRequestHandler(IOptions<RpcServerConfiguration> serverConfig,
			IRpcInvoker invoker,
			IRpcParser parser,
			IRpcResponseSerializer responseSerializer,
			ILogger<RpcRequestHandler> logger)
		{
			this.serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
			this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.responseSerializer = responseSerializer ?? throw new ArgumentNullException(nameof(responseSerializer));
			this.logger = logger;
		}

		public async Task<string> HandleRequestAsync(RpcPath requestPath, string requestBody, IRouteContext routeContext)
		{
			try
			{
				ParsingResult result = this.parser.ParseRequests(requestBody);
				this.logger?.LogInformation($"Processing {result.RequestCount} Rpc requests");

				int batchLimit = this.serverConfig.Value.BatchRequestLimit;
				List<RpcResponse> responses = new List<RpcResponse>();
				if (batchLimit > 0 && result.RequestCount > batchLimit)
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
					if (result.Requests.Any())
					{
						responses = await this.invoker.InvokeBatchRequestAsync(result.Requests, requestPath, routeContext);
					}
					else
					{
						responses = new List<RpcResponse>();
					}
					foreach ((RpcId id, RpcError error) in result.Errors)
					{
						if(id == default)
						{
							this.logger.LogError($"Request with no id failed and no response will be sent. Error - Code: {error.Code}, Message: {error.GetMessage(true)}");
							continue;
						}
						responses.Add(new RpcResponse(id, error));
					}
				}
				if (responses == null || !responses.Any())
				{
					this.logger?.LogInformation("No rpc responses created.");
					return null;
				}
				this.logger?.LogInformation($"{responses.Count} rpc response(s) created.");

				if (result.IsBulkRequest)
				{
					return this.responseSerializer.SerializeBulk(responses);
				}
				else
				{
					return this.responseSerializer.Serialize(responses.Single());
				}
			}
			catch (RpcException ex)
			{
				this.logger?.LogException(ex, "Error occurred when proccessing Rpc request. Sending Rpc error response");
				var response = new RpcResponse(null, ex.ToRpcError());
				return this.responseSerializer.Serialize(response);
			}
		}
	}
}
