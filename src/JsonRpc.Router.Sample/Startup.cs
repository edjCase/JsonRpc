using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using edjCase.JsonRpc.Router.Sample.RpcRoutes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.IO;

namespace edjCase.JsonRpc.Router.Sample
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
			services
				.AddJsonRpc()
				.AddRouting();
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddProvider(new DebugLoggerProvider());

			app.Use((httpContext, next) =>
			{
				KeyValuePair<string, StringValues> header = httpContext.Request.Headers.FirstOrDefault(h => h.Key == "Authorization");
				if (header.Equals(default(KeyValuePair<string, StringValues>)))
				{
					return null;
				}
				if (!header.Value.Any() || !header.Value.First().StartsWith("Basic "))
				{
					return null;
				}
				string headerValue = header.Value.First().Substring(6);
				byte[] valueBytes = Convert.FromBase64String(headerValue);
				string[] usernamePassword = Encoding.UTF8.GetString(valueBytes, 0, valueBytes.Length).Split(':');
				if (usernamePassword[0] == "Gekctek" && usernamePassword[1] == "Welc0me!")
				{
					return next();
				}
				return null;
			});

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

	public class Program
	{
		public static void Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
