using System.Linq;

namespace edjCase.JsonRpc.Router.Sample.RpcRoutes
{
	public class RpcString
	{
		public int CharacterCount(string text)
		{
			if (text == null)
			{
				return -1;
			}
			return text.Count();
		}
	}
}
