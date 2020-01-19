using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class MethanMatcherTests
	{
		private MethodInfo[] methods;
		public MethanMatcherTests()
		{
			this.methods = typeof(MethodMatcherController).GetMethods();
		}
		private DefaultRequestMatcher GetMatcher()
		{
			var logger = new Mock<ILogger<DefaultRequestMatcher>>(MockBehavior.Loose);
			var methodProvider = new Mock<IRpcMethodProvider>(MockBehavior.Strict);
			methodProvider
				.Setup(p => p.Get())
				.Returns(this.methods);

			return new DefaultRequestMatcher(logger.Object, methodProvider.Object);
		}


		[Fact]
		public void GetMatchingMethod_GuidParameter_Match()
		{
			Guid randomGuid = Guid.NewGuid();
			var parameters = new RpcParameters(new RawRpcParameter(RpcParameterType.String, randomGuid.ToString()));
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);
			var stringRequest = new RpcRequest("1", methodName, parameters);

			DefaultRequestMatcher matcher = this.GetMatcher();
			var requestSignature = RpcRequestSignature.Create(methodName, new[] { RpcParameterType.String });
			RpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo, methodInfo.MethodInfo);
			Assert.Single(methodInfo.Parameters);
			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(Guid), methodInfo.Parameters[0].RawType);
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[0].Type);
			Assert.Equal("guid", methodInfo.Parameters[0].Name);
		}

		public class MethodMatcherController
		{
			public Guid GuidTypeMethod(Guid guid)
			{
				return guid;
			}
		}
	}

}
