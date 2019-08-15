using EdjCase.JsonRpc.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EdjCase.JsonRpc.Router.Utilities
{
	public class RpcUtil
	{
		public static bool TypesMatch(object value, Type type)
		{
			Type nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
			{
				type = nullableType;
			}
			return value?.GetType() == type;
		}
	}
}
