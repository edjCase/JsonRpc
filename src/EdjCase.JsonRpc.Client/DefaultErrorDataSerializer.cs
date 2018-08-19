using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Client
{
	public interface IErrorDataSerializer
	{
		object Deserialize(int errorCode, string json);
	}

	public class DefaultErrorDataSerializer : IErrorDataSerializer
	{
		public object Deserialize(int errorCode, string json)
		{
			return json;
		}
	}
}
