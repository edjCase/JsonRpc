using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Sample.RpcRoutes
{
	[Authorize]
	[RpcRoute("Testz")]
	public class TestController : RpcController
	{
		public int Add(int a, int b)
		{
			return a + b;
		}
	}
}
