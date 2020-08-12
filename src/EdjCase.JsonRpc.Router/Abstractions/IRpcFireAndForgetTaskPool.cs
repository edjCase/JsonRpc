using System;
using System.Threading;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcFireAndForgetTaskPool
	{
		void Add(Func<Task> action);
		Task StopAndWaitTillAllCompleteAsync();
	}
}