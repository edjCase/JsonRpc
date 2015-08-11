using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router.Sample.RpcRoutes
{
	public class RpcString
	{
		public int CharacterCount(string text)
		{
			if (text == null)
			{
				return -1;
			}
			return text.Count();
		}
	}
}
