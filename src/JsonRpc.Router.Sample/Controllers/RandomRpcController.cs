using JsonRpc.Router;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Router.Sample.Controllers
{
	public class RandomRpcController
	{
		public int Test(string test)
		{
			return test.Count();
		}
	}
}
