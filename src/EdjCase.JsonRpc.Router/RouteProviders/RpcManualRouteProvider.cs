using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.RouteProviders
{
	public class RpcManualRouteProvider : IRpcRouteProvider
	{
		private IOptions<RpcManualRoutingOptions> Options { get; }

		public RpcManualRouteProvider(IOptions<RpcManualRoutingOptions> options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}


		public RpcPath BaseRequestPath => this.Options.Value.BaseRequestPath;


		/// <summary>
		/// Gets all the routes from all the cofiguration
		/// </summary>
		/// <returns>All the available routes</returns>
		public HashSet<RpcPath> GetRoutes()
		{
			if(this.Options.Value.Routes?.Keys != null)
			{
				return new HashSet<RpcPath>(this.Options.Value.Routes.Keys);
			}
			return new HashSet<RpcPath>();
		}

		/// <summary>
		/// Gets all the method providers for the specified path
		/// </summary>
		/// <param name="path">Path to the methods</param>
		/// <returns>All method providers for the specified path</returns>
		public List<IRpcMethodProvider> GetMethodsByPath(RpcPath path)
		{
			if(this.Options.Value.Routes == null 
				|| !this.Options.Value.Routes.TryGetValue(path, out List<IRpcMethodProvider> methods))
			{
				return new List<IRpcMethodProvider>();
			}
			return methods;
		}
	}
}
