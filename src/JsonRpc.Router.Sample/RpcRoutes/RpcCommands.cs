using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router.Sample.RpcRoutes
{
	public class RpcCommands
	{
		public bool ValidateId(Guid id)
		{
			return id != Guid.Empty;
		}

		private void HiddenMethod()
		{

		}
	}
}
