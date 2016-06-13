using System;

namespace edjCase.JsonRpc.Router.Sample.RpcRoutes
{
	public class RpcCommands
	{
		public bool ValidateId(Guid id)
		{
			return id != Guid.Empty;
		}
		
		public void Test()
		{

		}

		private void HiddenMethod()
		{

		}
	}
}
