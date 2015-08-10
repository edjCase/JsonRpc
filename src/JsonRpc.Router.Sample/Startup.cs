using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using JsonRpc.Router.Sample.RpcRoutes;

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
			app.UseJsonRpc(config =>
			{
				config.RegisterClassToRpcRoute<RpcString>("Strings");
				config.RegisterClassToRpcRoute<RpcCommands>("Commands");
				config.RegisterClassToRpcRoute<RpcMath>("Math");
			});
		}
	}
}
