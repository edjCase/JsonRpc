using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Extensions.Configuration;
using System.Net.WebSockets;
using System.Threading;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore;
using System.Reflection;

namespace EdjCase.JsonRpc.Router.Sample
{
	public class Startup
	{
		// This method gets called by a runtime.
		// Use this method to add services to the container
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddJsonRpc(config =>
				{
					//(Optional) Hard cap on batch size, will block requests will larger sizes, defaults to no limit
					config.BatchRequestLimit = 5;
					//(Optional) If true returns full error messages in response, defaults to false
					config.ShowServerExceptions = false;
					//(Optional) Configure how the router serializes requests
					config.JsonSerializerSettings = new System.Text.Json.JsonSerializerOptions
					{
						//Example json config
						IgnoreNullValues = false,
						WriteIndented = true
					};
					//(Optional) Configure custom exception handling for exceptions during invocation of the method
					config.OnInvokeException = (context) =>
					{
						if (context.Exception is InvalidOperationException)
						{
							//Handle a certain type of exception and return a custom response instead
							//of an internal server error
							int customErrorCode = 1;
							var customData = new
							{
								Field = "Value"
							};
							var response = new RpcMethodErrorResult(customErrorCode, "Custom message", customData);
							return OnExceptionResult.UseObjectResponse(response);
						}
						//Continue to throw the exception
						return OnExceptionResult.DontHandle();
					};
				});
		}

		// Configure is called after ConfigureServices is called.
		public void Configure(IApplicationBuilder app)
		{
			app
				.Map("/BaseController", b =>
				{
					//Will make all controllers that derived from `ControllerBase` available
					//Each dervied controller will use their name as the route, unless overridden by RpcRouteAttribute
					b.UseJsonRpcWithBaseController<ControllerBase>();
				})
				.Map("/Controllers", b =>
				{
					b.UseJsonRpc(options =>
					{
						options
							//Will make controller methods available for path '/First', unless overridden by RpcRouteAttribute
							//Not that any class will work here, not just a class derived from `RpcController`
							.AddController<NonRpcController>()
							//Will make `CustomController` methods available for custom path '/CustomPath'
							.AddControllerWithCustomPath<CustomController>("CustomPath");
					});
				})
				.Map("/Methods", b =>
				{
					b.UseJsonRpc(options =>
					{
						MethodInfo customControllerMethod1 = typeof(CustomController).GetMethod("Method1");
						MethodInfo otherControllerMethod1 = typeof(OtherController).GetMethod("Method1");
						options
							//Will make the `Method1` method in `CustomController` available with route '/'
							//Note that since that method has `RpcRouteAttribute("Method")`, that will change the method name
							//from `Method1` to `Method` in the router
							.AddMethod(customControllerMethod1)
							//Will make the `Method1` method in `OtherController` available with route '/CustomMethods'
							.AddMethod(otherControllerMethod1, "CustomMethods");
					});

				})
				//Will make all public classes deriving from `RpcController` available to the rpc router
				.UseJsonRpc();

		}
	}

	public class Program
	{
		public static void Main(string[] args)
		{
			var host = WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
