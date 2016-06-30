using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Utilities
{

	public static class RouteContextExtensions
	{
		public static void MarkAsHandled(this RouteContext context)
		{
			context.Handler = c => Task.FromResult(0);
		}
	}
}
