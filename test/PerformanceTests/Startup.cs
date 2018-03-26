using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PerformanceTests
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddJsonRpc();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory factory)
		{
			app
				.Map("/Tests", builder =>
				{
					builder.Run((handler) =>
					{
						return TestRunner.RunInvokerAsync();
					});
				})
				.UseJsonRpc(options =>
				{
				});
		}
	}


	public class Program
	{
		public static void Main(string[] args)
		{
			//TestRunner.RunInvokerAsync().GetAwaiter().GetResult();
			//TestRunner.RunCompression();

			IConfiguration config = new ConfigurationBuilder()
				.AddCommandLine(args)
				.Build();

			new WebHostBuilder()
				.UseKestrel()
				.UseConfiguration(config)
				.UseStartup<Startup>()
				.Build()
				.Run();
		}
	}
}
