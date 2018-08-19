using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Sample.RpcRoutes
{
	public abstract class ControllerBase : RpcController
	{

	}

	//[Authorize]
	[RpcRoute("")]
	public class PerformanceController : ControllerBase
	{
		
		public void Empty()
		{

		}
		public IntegerFromSpace CharacterCount(string text)
		{
			if (text == null)
			{
				return new IntegerFromSpace()
				{
					Test = -1
				};
			}
			return new IntegerFromSpace()
			{
				Test = text.Count()
			};
		}

	}
	public enum TestEnum
	{
		Test1,
		Test2
	}
}
