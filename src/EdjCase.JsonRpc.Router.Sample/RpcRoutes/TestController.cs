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
		public async Task<int> Add(int a, int b)
		{
			await Task.Delay(1);
			throw new Exception("Test");
		}
	}
}
