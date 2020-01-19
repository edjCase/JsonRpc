using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
	internal class RpcRequestHandler : IRpcRequestHandler
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

		public async Task<bool> HandleRequestAsync(Stream requestBody, Stream responseBody)
		{
			try
			{
				ParsingResult result = this.parser.ParseRequests(requestBody);
				this.logger.ProcessingRequests(result.RequestCount);

				int? batchLimit = this.serverConfig.Value.BatchRequestLimit;
				List<RpcResponse> responses = new List<RpcResponse>();
				if (batchLimit > 0 && result.RequestCount > batchLimit)
				{
					string batchLimitError = $"Request count exceeded batch request limit ({batchLimit}).";
					responses = new List<RpcResponse>
					{
						new RpcResponse(new RpcId(), new RpcError(RpcErrorCode.InvalidRequest, batchLimitError))
					};
					this.logger.LogError(batchLimitError + " Returning error response.");
				}
				else
				{
					if (result.Requests.Any())
					{
						responses = await this.invoker.InvokeBatchRequestAsync(result.Requests);
					}
					else
					{
						responses = new List<RpcResponse>();
					}
					foreach ((RpcId id, RpcError error) in result.Errors)
					{
						if (id == default)
						{
							this.logger.ResponseFailedWithNoId(error.Code, error.Message);
							continue;
						}
						responses.Add(new RpcResponse(id, error));
					}
				}
				if (responses == null || !responses.Any())
				{
					this.logger.NoResponses();
					return false;
				}
				this.logger.Responses(responses.Count);

				if (result.IsBulkRequest)
				{
					await this.responseSerializer.SerializeBulkAsync(responses, responseBody);
				}
				else
				{
					await this.responseSerializer.SerializeAsync(responses.Single(), responseBody);
				}
			}
			catch (RpcException ex)
			{
				this.logger.LogException(ex, "Error occurred when proccessing Rpc request. Sending Rpc error response");
				var response = new RpcResponse(new RpcId(), ex.ToRpcError(this.serverConfig.Value.ShowServerExceptions));
				await this.responseSerializer.SerializeAsync(response, responseBody);
			}
			return true;
		}
	}
}
