using System.Reflection;
using EdjCase.JsonRpc.Router.Abstractions;

namespace EdjCase.JsonRpc.Router.Swagger.Models
{
	public class UniqueMethod
	{
		public string UniqueUrl { get; }
		public IRpcMethodInfo Info { get; }

		public UniqueMethod(string uniqueUrl, IRpcMethodInfo info)
		{
			this.UniqueUrl = uniqueUrl;
			this.Info = info;
		}
	}
}