using System;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.Logging;

namespace EdjCase.JsonRpc.Router.Tests
{
	internal class FakeLogger<T> : ILogger<T>
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return new FakeDisposable();
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return false;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{

		}
	}

	internal class FakeDisposable : IDisposable
	{
		public void Dispose()
		{

		}
	}
}