using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EdjCase.JsonRpc.Router.Sample
{
	public class DebugLogger : ILogger
	{
		public string Name { get; }
		public LogLevel LogLevel { get; set; }
		public DebugLogger(string name, LogLevel logLevel = LogLevel.Debug)
		{
			this.Name = name;
			this.LogLevel = logLevel;
		}
		
		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel >= this.LogLevel;
		}
		
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!this.IsEnabled(logLevel))
			{
				return;
			}
			string formattedException = formatter.Invoke(state, exception);
			string logMessage = $"[{logLevel}] " + formattedException;
			Debug.WriteLine(logMessage);
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}
	}
}
