using System;

namespace EdjCase.JsonRpc.Router.Sample.RpcRoutes
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

		public string Optional(string test = null)
		{
			return test;
		}
	}
}
