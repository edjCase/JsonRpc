using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using JsonRpc.Router.Sample.RpcRoutes;
using Microsoft.Framework.Logging;

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
			services.AddJsonRpc();
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.MinimumLevel = LogLevel.Debug;
			loggerFactory.AddProvider(new DebugLoggerProvider());
			app.UseJsonRpc(config =>
			{
				config.RoutePrefix = "RpcApi";
				config.RegisterClassToRpcRoute<RpcMath>();
				config.RegisterClassToRpcRoute<RpcString>("Strings");
				config.RegisterClassToRpcRoute<RpcCommands>("Commands");
				config.RegisterClassToRpcRoute<RpcMath>("Math");
			});
		}
	}
}
