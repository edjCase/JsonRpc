using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Utilities
{
	internal static class TypeExtensions
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

		public static JsonWriterOptions ToWriterOptions(this JsonSerializerOptions? options)
		{
			JsonWriterOptions writerOptions = default;
			if (options != null)
			{
				writerOptions.Encoder = options.Encoder;
				writerOptions.Indented = options.WriteIndented;
			}
			return writerOptions;
		}
	}

	internal static class RouteContextExtensions
	{
		public static void MarkAsHandled(this RouteContext context)
		{
			context.Handler = c => Task.FromResult(0);
		}
	}
}
