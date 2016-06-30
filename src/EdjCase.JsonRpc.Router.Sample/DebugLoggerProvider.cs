using Microsoft.Extensions.Logging;

namespace EdjCase.JsonRpc.Router.Sample
{
	public class DebugLoggerProvider : ILoggerProvider
	{
		public ILogger CreateLogger(string name)
		{
			return new DebugLogger(name);
		}

		public void Dispose()
		{
			
		}
	}
}
