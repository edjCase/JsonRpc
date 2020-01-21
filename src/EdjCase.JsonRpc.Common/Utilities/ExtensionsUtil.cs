using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Common.Utilities
{
	public static class TypeExtensions
	{
		/// <summary>
		/// Determines if the type is a number
		/// </summary>
		/// <param name="type">Type of the object</param>
		/// <param name="includeInteger">Includes a check for whole number types. Defaults to true</param>
		/// <returns>True if the type is a number, otherwise False</returns>
		public static bool IsNumericType(this Type type, bool includeInteger = true)
		{
			if (includeInteger)
			{
				return type == typeof(long)
						|| type == typeof(int)
						|| type == typeof(short)
						|| type == typeof(float)
						|| type == typeof(double)
						|| type == typeof(decimal);
			}
			else
			{
				return type == typeof(float)
						|| type == typeof(double)
						|| type == typeof(decimal);
			}
		}
	}
}
