using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Utilities
{
	public static class TypeExtensions
	{
		/// <summary>
		/// Determines if the type is nullable
		/// </summary>
		/// <param name="type">Type of the object</param>
		/// <returns>True if the type is nullable, otherwise False</returns>
		public static bool IsNullableType(this Type type)
		{
			return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
		}
	}

	public static class RouteContextExtensions
	{
		public static void MarkAsHandled(this RouteContext context)
		{
			context.Handler = c => Task.FromResult(0);
		}
	}

	public static class LoggerExtensions
	{
		public static void LogException(this ILogger logger, Exception ex, string message = null)
		{
			//Log error ignores the exception for some reason
			if (message != null)
			{
				message = $"{message}{Environment.NewLine}{ex}";
			}
			else
			{
				message = $"{ex}";
			}
			logger.LogError(new EventId(), ex, message);
		}
	}
}
