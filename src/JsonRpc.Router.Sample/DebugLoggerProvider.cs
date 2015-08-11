using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router.Sample
{
	public class DebugLoggerProvider : ILoggerProvider
	{
		public ILogger CreateLogger(string name)
		{
			return new DebugLogger(name);
		}
	}
}
