using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Defaults
{
	internal class DefaultAuthorizationHandler : IRpcAuthorizationHandler
	{
		private ILogger<DefaultAuthorizationHandler> logger { get; }
		/// <summary>
		/// Provides authorization policies for the authroziation service
		/// </summary>
		private IAuthorizationPolicyProvider policyProvider { get; }
		private IHttpContextAccessor httpContextAccessor { get; }

		/// <summary>
		/// AspNet service to authorize requests
		/// </summary>
		private IAuthorizationService authorizationService { get; }
		public DefaultAuthorizationHandler(ILogger<DefaultAuthorizationHandler> logger,
			IAuthorizationPolicyProvider policyProvider,
			IAuthorizationService authorizationService,
			IHttpContextAccessor httpContextAccessor)
		{
			this.logger = logger;
			this.policyProvider = policyProvider;
			this.authorizationService = authorizationService;
			this.httpContextAccessor = httpContextAccessor;
		}
		public async Task<bool> IsAuthorizedAsync(IRpcMethodInfo methodInfo)
		{
			if (methodInfo.AuthorizeDataList.Any())
			{
				if (methodInfo.AllowAnonymous)
				{
					this.logger.SkippingAuth();
				}
				else
				{
					this.logger.RunningAuth();
					AuthorizationResult authResult = await this.CheckAuthorize(methodInfo.AuthorizeDataList);
					if (authResult.Succeeded)
					{
						this.logger.AuthSuccessful();
					}
					else
					{
						this.logger.AuthFailed();
						return false;
					}
				}
			}
			else
			{
				this.logger.NoConfiguredAuth();
			}
			return true;
		}

		private async Task<AuthorizationResult> CheckAuthorize(IReadOnlyList<IAuthorizeData> authorizeDataList)
		{
			if (!authorizeDataList.Any())
			{
				return AuthorizationResult.Success();
			}
			AuthorizationPolicy policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeDataList);
			ClaimsPrincipal claimsPrincipal = this.httpContextAccessor.HttpContext.User;
			return await this.authorizationService.AuthorizeAsync(claimsPrincipal, policy);
		}


	}
}
