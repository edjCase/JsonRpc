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
		public string Method1(string a)
		{
			return a;
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
