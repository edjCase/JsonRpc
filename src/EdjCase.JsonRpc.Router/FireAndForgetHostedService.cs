using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EdjCase.JsonRpc.Router
{
	internal class FireAndForgetHostedService : IHostedService
	{
		private readonly ILogger<FireAndForgetHostedService> logger;
		private readonly IRpcFireAndForgetTaskPool taskPool;

		public FireAndForgetHostedService(ILogger<FireAndForgetHostedService> logger,
			IRpcFireAndForgetTaskPool taskPool)
		{
			this.logger = logger;
			this.taskPool = taskPool;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return taskPool.StopAndWaitTillAllCompleteAsync();
		}
	}
}