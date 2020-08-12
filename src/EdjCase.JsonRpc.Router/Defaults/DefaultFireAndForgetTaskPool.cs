using System;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using EdjCase.JsonRpc.Router.Utilities;

namespace EdjCase.JsonRpc.Router.Defaults
{
	internal class DefaultFireAndForgetTaskPool : IRpcFireAndForgetTaskPool
	{
		private readonly ConcurrentDictionary<Guid, Task> queue;
		private readonly ILogger<DefaultFireAndForgetTaskPool> logger;
		private bool stopped;
		public DefaultFireAndForgetTaskPool(ILogger<DefaultFireAndForgetTaskPool> logger)
		{
			this.queue = new ConcurrentDictionary<Guid, Task>();
			this.logger = logger;
			this.stopped = false;
		}

		public void Add(Func<Task> action)
		{
			if (this.stopped)
			{
				//Safeguard on shutdown
				throw new RpcCanceledRequestException("Application is shutting down, cannot process more requests.");
			}
			Guid taskId = Guid.NewGuid();
			void Run()
			{
				try
				{
					action().GetAwaiter().GetResult();
				}
				catch (Exception ex)
				{
					this.logger.LogException(ex, "Fire and forget task failed");
				}
				bool removed = this.queue.TryRemove(taskId, out Task _);
				if (!removed)
				{
					this.logger.LogWarning("Unable to cleanup task from background task pool, was already removed.");
					return;
				}
				this.logger.LogDebug($"Finished task '{taskId}'");

			}
			// void Cleanup(Task completedTask)
			// {
			// 	bool removed = this.queue.TryRemove(taskId, out Task _);
			// 	if (!removed)
			// 	{
			// 		this.logger.LogWarning("Unable to cleanup task from background task pool, was already removed.");
			// 		return;
			// 	}
			// 	if (completedTask.IsFaulted)
			// 	{
			// 		this.logger.LogException(completedTask.Exception, "Fire and forget task failed");
			// 	}
			// 	this.logger.LogDebug($"Finished task '{taskId}'");
			// }
			//TODO long running?
			Task runningTask = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
			//TODO continue with?
			//runningTask.ContinueWith(Cleanup);
			bool added = this.queue.TryAdd(taskId, runningTask);
			if (!added)
			{
				throw new RpcUnknownException("Unable to add the task to the background task pool.");
			}
		}

		public async Task StopAndWaitTillAllCompleteAsync()
		{
			this.stopped = true;
			Task[] remainingTasks = this.queue.Values.ToArray();
			await Task.WhenAll(remainingTasks);
			int a = 1;
		}
	}
}