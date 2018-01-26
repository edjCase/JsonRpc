using System;
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
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

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
			loggerFactory
				.AddDebug(LogLevel.Debug)
				.AddConsole(LogLevel.Debug);

			app.UseAuthentication();

			app.Map("/RpcApi", rpcApp =>
			{
				rpcApp
				.Use(this.LogBody)
				.UseManualJsonRpc(builder =>
				{
					builder.RegisterController<RpcMath>();
					builder.RegisterController<RpcString>("Strings");
					builder.RegisterController<RpcCommands>("Commands");
					builder.RegisterController<RpcMath>("Math");
				});
			})
			.Use(this.LogBody)
			.UseJsonRpc(builder =>
			{
				builder.BaseControllerType = typeof(ControllerBase);
				builder.BaseRequestPath = "Auto";
			});

		}

		public async Task LogBody(HttpContext context, Func<Task> next)
		{
			ILogger<Startup> logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();
			using (MemoryStream newRequestStream = new MemoryStream())
			{
				Stream requestStream = context.Request.Body;
				context.Request.Body.CopyTo(newRequestStream);
				newRequestStream.Seek(0, SeekOrigin.Begin);
				string requestBody = new StreamReader(newRequestStream).ReadToEnd();
				logger.LogInformation(requestBody);

				newRequestStream.Seek(0, SeekOrigin.Begin);
				context.Request.Body = newRequestStream;

				using (MemoryStream newBodyStream = new MemoryStream())
				{
					Stream bodyStream = context.Response.Body;
					context.Response.Body = newBodyStream;

					await next();

					newBodyStream.Seek(0, SeekOrigin.Begin);

					StreamReader reader = new StreamReader(newBodyStream);
					string body = reader.ReadToEnd();
					logger.LogInformation(body);

					newBodyStream.Seek(0, SeekOrigin.Begin);
					newBodyStream.CopyTo(bodyStream);
					context.Response.Body = bodyStream;
				}
			}
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
