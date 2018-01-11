using EdjCase.JsonRpc.Core.Utilities;
using Newtonsoft.Json.Linq;
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
			if (value is JToken token)
			{
				switch (token.Type)
				{
					case JTokenType.Array:
						JArray jArray = (JArray)token;
						return type.IsArray
						|| type.GetInterfaces()
							.Any(inter => inter.IsGenericType
							&& inter.GetGenericTypeDefinition() == typeof(IEnumerable<>)
							&& (!token.Any()
							|| RpcUtil.TypesMatch(jArray.First(), inter.GenericTypeArguments[0])));
					case JTokenType.Boolean:
						return type == typeof(bool);
					case JTokenType.Bytes:
						return type == typeof(byte[]);
					case JTokenType.Date:
						return type == typeof(DateTime);
					case JTokenType.Guid:
						return type == typeof(Guid);
					case JTokenType.Float:
						return type.IsNumericType(includeInteger: false);
					case JTokenType.Integer:
						return type.IsNumericType();
					case JTokenType.Null:
					case JTokenType.Undefined:
						return nullableType != null || type.IsNullableType();
					case JTokenType.Object:
						return type.IsAssignableFrom(typeof(object));
					case JTokenType.Uri:
						return type == typeof(string)
							|| type == typeof(Uri);
					case JTokenType.String:
						return type == typeof(string);
					case JTokenType.TimeSpan:
						return type == typeof(TimeSpan)
							|| type == typeof(double)
							|| type == typeof(decimal);
					default:
						return false;
				}
			}
			else
			{
				return value?.GetType() == type;
			}
		}
	}
}
