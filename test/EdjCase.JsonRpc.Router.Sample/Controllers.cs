using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Sample
{
	public abstract class ControllerBase : RpcController
	{

	}

	[RpcRoute("Main")]
	public class CustomController : ControllerBase
	{
		[RpcRoute("Method")]
		public string Method1(string a)
		{
			return a;
		}
	}
	public class OtherController : ControllerBase
	{
		private static int counter = 0;
		public async Task<int> Method1(string a)
		{
			if (a == "delay")
			{
				await Task.Delay(20000);
			}
			OtherController.counter++;
			return OtherController.counter;
		}
	}
	public class Commands : ControllerBase
	{
		public string Method1(string a)
		{
			return a;
		}
	}

	public class NonRpcController
	{
		public string Method1(string a)
		{
			return a;
		}
	}
}
