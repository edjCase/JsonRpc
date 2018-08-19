using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.MethodProviders;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.RouteProviders
{
	public class RpcSingleRouteProvider : IRpcRouteProvider
	{
		public IOptions<SingleRouteOptions> Options { get; }
		public RpcPath BaseRequestPath { get; }

		public RpcSingleRouteProvider(IOptions<SingleRouteOptions> options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
			this.BaseRequestPath = this.Options.Value?.BaseRequestPath ?? RpcPath.Default;
		}


		/// <summary>
		/// Gets all the method providers for the specified path
		/// </summary>
		/// <param name="path">Path to the methods</param>
		/// <returns>All method providers for the specified path</returns>
		public List<IRpcMethodProvider> GetMethodsByPath(RpcPath path) => this.Options.Value?.MethodProviders ?? new List<IRpcMethodProvider>();
	}

	public class SingleRouteOptions
	{
		public List<IRpcMethodProvider> MethodProviders { get; set; } = new List<IRpcMethodProvider>();
		public RpcPath BaseRequestPath { get; set; } = RpcPath.Default;

		public void AddClass<T>()
		{
			this.MethodProviders.Add(new ControllerPublicMethodProvider(typeof(T)));
		}
	}
}
