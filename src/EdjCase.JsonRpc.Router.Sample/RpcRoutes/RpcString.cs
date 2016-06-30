using System.Linq;

namespace EdjCase.JsonRpc.Router.Sample.RpcRoutes
{
	public class RpcString
	{
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

	public class IntegerFromSpace
	{
		public int Test { get; set; }
	}
}
