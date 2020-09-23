using EdjCase.JsonRpc.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EdjCase.JsonRpc.Router.Utilities
{
	internal class RpcUtil
	{
		public static bool TypesMatch(object value, Type type)
		{
			Type? nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
			{
				type = nullableType;
			}
			return value?.GetType() == type;
		}

		public static bool NamesMatch(ReadOnlySpan<char> actual, ReadOnlySpan<char> requested)
		{
			//Requested can be longer because it could have - or _ characters
			if (actual.Length > requested.Length)
			{
				return false;
			}
			int j = 0;
			int equalsChars = 0;
			for (int i = 0; i < actual.Length && j < requested.Length; i++)
			{
				char requestedChar = requested[j++];
				char actualChar = actual[i];
				if (char.ToLowerInvariant(actualChar) == char.ToLowerInvariant(requestedChar))
				{
					equalsChars++;
					continue;
				}
				if (requestedChar == '-' || requestedChar == '_')
				{
					//Skip this j
					i--;
					continue;
				}
				return false;
			}
			//Make sure that it matched ALL the actual characters
			//j - all iterations of comparing, need compare j with requested.Length
			return j == requested.Length
					&& equalsChars == actual.Length;
		}
	}
}
