using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using System.Collections.Generic;
using System;
using JsonRpc.Router.Sample.Controllers;

namespace JsonRpc.Router.Sample
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
		}

		// This method gets called by a runtime.
		// Use this method to add services to the container
		public void ConfigureServices(IServiceCollection services)
		{
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			app.UseJsonRpc(this.ConfigureRouter);
		}

		private void ConfigureRouter(RpcRouterConfiguration configuration)
		{
			configuration.RegisterRpcGroup<RandomRpcController>("Test");
		}
	}
}
