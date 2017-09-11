using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

		/// <param name="serverConfig">Configuration data for the server</param>
		/// <param name="invoker">Component that invokes Rpc requests target methods and returns a response</param>
		/// <param name="parser">Component that parses Http requests into Rpc requests</param>
		/// <param name="logger">Component that logs actions from the router</param>
		public RpcRequestHandler(IOptions<RpcServerConfiguration> serverConfig,
			IRpcInvoker invoker,
			IRpcParser parser,
			ILogger<RpcRequestHandler> logger)
		{
			this.serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
			this.invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.logger = logger;
		}

		public async Task<string> HandleRequestAsync(RpcPath requestPath, string requestBody, IRouteContext routeContext)
		{
			try
			{
				List<RpcRequest> requests = this.parser.ParseRequests(requestBody, out bool isBulkRequest, this.serverConfig.Value.JsonSerializerSettings);
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
					responses = await this.invoker.InvokeBatchRequestAsync(requests, requestPath, routeContext);
				}
				if (responses == null || !responses.Any())
				{
					this.logger?.LogInformation("No rpc responses created.");
					return null;
				}

				string resultJson = !isBulkRequest
					? JsonConvert.SerializeObject(responses.Single(), this.serverConfig.Value.JsonSerializerSettings)
					: JsonConvert.SerializeObject(responses, this.serverConfig.Value.JsonSerializerSettings);


				this.logger?.LogInformation($"{responses.Count} rpc response(s) created.");
				return resultJson;
			}
			catch (RpcException ex)
			{
				this.logger?.LogException(ex, "Error occurred when proccessing Rpc request. Sending Rpc error response");
				var response = new RpcResponse(null, new RpcError(ex, this.serverConfig.Value.ShowServerExceptions));
				return JsonConvert.SerializeObject(response, this.serverConfig.Value.JsonSerializerSettings);
			}
		}
	}
}
