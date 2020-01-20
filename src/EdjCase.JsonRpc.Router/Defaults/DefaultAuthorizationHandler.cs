using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Defaults
{
	internal class DefaultAuthorizationHandler : IRpcAuthorizationHandler
	{
		private static ConcurrentDictionary<Type, (List<IAuthorizeData>, bool)> classAttributeCache { get; } = new ConcurrentDictionary<Type, (List<IAuthorizeData>, bool)>();
		private static ConcurrentDictionary<RpcMethodInfo, (List<IAuthorizeData>, bool)> methodAttributeCache { get; } = new ConcurrentDictionary<RpcMethodInfo, (List<IAuthorizeData>, bool)>();
		private ILogger<DefaultAuthorizationHandler> logger { get; }
		/// <summary>
		/// Provides authorization policies for the authroziation service
		/// </summary>
		private IAuthorizationPolicyProvider policyProvider { get; }
		private IRpcContextAccessor contextAccessor { get; }

		/// <summary>
		/// AspNet service to authorize requests
		/// </summary>
		private IAuthorizationService authorizationService { get; }
		public DefaultAuthorizationHandler(ILogger<DefaultAuthorizationHandler> logger,
			IAuthorizationPolicyProvider policyProvider,
			IRpcContextAccessor contextAccessor,
			IAuthorizationService authorizationService)
		{
			this.logger = logger;
			this.policyProvider = policyProvider;
			this.contextAccessor = contextAccessor;
			this.authorizationService = authorizationService;
		}
		public async Task<bool> IsAuthorizedAsync(RpcMethodInfo methodInfo)
		{
			(List<IAuthorizeData> authorizeDataListClass, bool allowAnonymousOnClass) = DefaultAuthorizationHandler.classAttributeCache.GetOrAdd(methodInfo.MethodInfo.DeclaringType, GetClassAttributeInfo);
			(List<IAuthorizeData> authorizeDataListMethod, bool allowAnonymousOnMethod) = DefaultAuthorizationHandler.methodAttributeCache.GetOrAdd(methodInfo, GetMethodAttributeInfo);

			if (authorizeDataListClass.Any() || authorizeDataListMethod.Any())
			{
				if (allowAnonymousOnClass || allowAnonymousOnMethod)
				{
					this.logger.SkippingAuth();
				}
				else
				{
					this.logger.RunningAuth();
					IRpcContext context = this.contextAccessor.Value!;
					AuthorizationResult authResult = await this.CheckAuthorize(authorizeDataListClass, context);
					if (authResult.Succeeded)
					{
						//Have to pass both controller and method authorize
						authResult = await this.CheckAuthorize(authorizeDataListMethod, context);
					}
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

			//functions
			(List<IAuthorizeData> Data, bool allowAnonymous) GetClassAttributeInfo(Type type)
			{
				return GetAttributeInfo(type.GetCustomAttributes());
			}

			(List<IAuthorizeData> Data, bool allowAnonymous) GetMethodAttributeInfo(RpcMethodInfo info)
			{
				return GetAttributeInfo(info.MethodInfo.GetCustomAttributes());
			}
			(List<IAuthorizeData> Data, bool allowAnonymous) GetAttributeInfo(IEnumerable<Attribute> attributes)
			{
				bool allowAnonymous = false;
				var dataList = new List<IAuthorizeData>(10);
				foreach (Attribute attribute in attributes)
				{
					if (attribute is IAuthorizeData data)
					{
						dataList.Add(data);
					}
					if (!allowAnonymous && attribute is IAllowAnonymous)
					{
						allowAnonymous = true;
					}
				}
				return (dataList, allowAnonymous);
			}
		}

		private async Task<AuthorizationResult> CheckAuthorize(List<IAuthorizeData> authorizeDataList, IRpcContext routeContext)
		{
			if (!authorizeDataList.Any())
			{
				return AuthorizationResult.Success();
			}
			AuthorizationPolicy policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeDataList);
			return await this.authorizationService.AuthorizeAsync(routeContext.User, policy);
		}


	}
}
