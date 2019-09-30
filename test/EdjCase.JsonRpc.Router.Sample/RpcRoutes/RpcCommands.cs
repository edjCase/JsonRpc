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

        public object Test(string name, int i, double j, string nullable, TestObj obj)
        {
            return new { name, i, j, nullable, obj };
        }

        public class TestObj
        {
            public int Derp { get; set; }
        }

		private void HiddenMethod()
		{

		}

		public string Optional(string test = null)
		{
			return test;
		}

		public string Optional(int i, string test = null)
		{
			return i + test;
		}
	}
}
