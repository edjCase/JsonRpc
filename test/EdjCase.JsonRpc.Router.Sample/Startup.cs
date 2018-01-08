﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdjCase.JsonRpc.Router.Sample.RpcRoutes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;

namespace EdjCase.JsonRpc.Router.Sample
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
				.AddAuthentication("Basic")
				.AddBasicAuth(options =>
				{
					options.AuthenticateCredential = authInfo =>
					{
						if (authInfo.Credential.Username == "Gekctek" && authInfo.Credential.Password == "Welc0me!")
						{
							var claims = new List<Claim>
							{
								new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
							};
							var identity = new ClaimsIdentity(claims, "Basic");
							var principal = new ClaimsPrincipal(identity);
							var properties = new AuthenticationProperties();
							return Task.FromResult(new AuthenticationTicket(principal, properties, "Basic"));
						}
						return Task.FromResult<AuthenticationTicket>(null);
					};

				});
			services
				.AddJsonRpc(config =>
				{
					config.ShowServerExceptions = true;
					config.BatchRequestLimit = 1;
				});
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddProvider(new DebugLoggerProvider());

			app.UseAuthentication();

			app.Map("/RpcApi", rpcApp =>
			{
				rpcApp.UseManualJsonRpc(builder =>
				{
					builder.RegisterController<RpcMath>();
					builder.RegisterController<RpcString>("Strings");
					builder.RegisterController<RpcCommands>("Commands");
					builder.RegisterController<RpcMath>("Math");
				});
			});

			app.UseJsonRpc(builder =>
			{
				builder.BaseControllerType = typeof(ControllerBase);
				builder.BaseRequestPath = "Auto";
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
