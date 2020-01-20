using EdjCase.JsonRpc.Router.Abstractions;
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
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);

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




		[Fact]
		public void GetMatchingMethod_SimpleMulitParam_DictMatch()
		{
			DefaultRequestMatcher matcher = this.GetMatcher();

			var parameters = new Dictionary<string, RpcParameterType>
			{
				{"a", RpcParameterType.Number },
				{"b", RpcParameterType.Boolean },
				{"c", RpcParameterType.String },
				{"d", RpcParameterType.Object },
				{"e", RpcParameterType.Null }
			};
			string methodName = nameof(MethodMatcherController.SimpleMulitParam);
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			RpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo, methodInfo.MethodInfo);
			Assert.Equal(5, methodInfo.Parameters.Length);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(int), methodInfo.Parameters[0].RawType);
			Assert.Equal(RpcParameterType.Number, methodInfo.Parameters[0].Type);
			Assert.Equal("a", methodInfo.Parameters[0].Name);

			Assert.False(methodInfo.Parameters[1].IsOptional);
			Assert.Equal(typeof(bool), methodInfo.Parameters[1].RawType);
			Assert.Equal(RpcParameterType.Boolean, methodInfo.Parameters[1].Type);
			Assert.Equal("b", methodInfo.Parameters[1].Name);

			Assert.False(methodInfo.Parameters[2].IsOptional);
			Assert.Equal(typeof(string), methodInfo.Parameters[2].RawType);
			Assert.Equal(RpcParameterType.String, methodInfo.Parameters[2].Type);
			Assert.Equal("c", methodInfo.Parameters[2].Name);

			Assert.False(methodInfo.Parameters[3].IsOptional);
			Assert.Equal(typeof(object), methodInfo.Parameters[3].RawType);
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[3].Type);
			Assert.Equal("d", methodInfo.Parameters[3].Name);

			Assert.True(methodInfo.Parameters[4].IsOptional);
			Assert.Equal(typeof(int?), methodInfo.Parameters[4].RawType);
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[4].Type);
			Assert.Equal("e", methodInfo.Parameters[4].Name);
		}
		[Fact]
		public void GetMatchingMethod_SimpleMulitParam_ListMatch()
		{
			DefaultRequestMatcher matcher = this.GetMatcher();

			RpcParameterType[] parameters = new[] { RpcParameterType.Number, RpcParameterType.Boolean, RpcParameterType.String, RpcParameterType.Object, RpcParameterType.Null };
			string methodName = nameof(MethodMatcherController.SimpleMulitParam);
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			RpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo, methodInfo.MethodInfo);
			Assert.Equal(5, methodInfo.Parameters.Length);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(int), methodInfo.Parameters[0].RawType);
			Assert.Equal(RpcParameterType.Number, methodInfo.Parameters[0].Type);
			Assert.Equal("a", methodInfo.Parameters[0].Name);

			Assert.False(methodInfo.Parameters[1].IsOptional);
			Assert.Equal(typeof(bool), methodInfo.Parameters[1].RawType);
			Assert.Equal(RpcParameterType.Boolean, methodInfo.Parameters[1].Type);
			Assert.Equal("b", methodInfo.Parameters[1].Name);

			Assert.False(methodInfo.Parameters[2].IsOptional);
			Assert.Equal(typeof(string), methodInfo.Parameters[2].RawType);
			Assert.Equal(RpcParameterType.String, methodInfo.Parameters[2].Type);
			Assert.Equal("c", methodInfo.Parameters[2].Name);

			Assert.False(methodInfo.Parameters[3].IsOptional);
			Assert.Equal(typeof(object), methodInfo.Parameters[3].RawType);
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[3].Type);
			Assert.Equal("d", methodInfo.Parameters[3].Name);

			Assert.True(methodInfo.Parameters[4].IsOptional);
			Assert.Equal(typeof(int?), methodInfo.Parameters[4].RawType);
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[4].Type);
			Assert.Equal("e", methodInfo.Parameters[4].Name);
		}

		public class MethodMatcherController
		{
			public Guid GuidTypeMethod(Guid guid)
			{
				return guid;
			}

			public (int, bool, string, object, int?) SimpleMulitParam(int a, bool b, string c, object d, int? e = null)
			{
				return (a, b, c, d, e);
			}
		}
	}

}
