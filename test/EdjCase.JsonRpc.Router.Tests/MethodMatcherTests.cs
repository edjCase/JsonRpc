using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Tests.Controllers;
using EdjCase.JsonRpc.Router.Utilities;
using Xunit;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class MethodMatcherTests
	{
		private Dictionary<RpcPath, List<MethodInfo>> methodData;
		private Mock<IRpcContext> rpcContext;

		public MethodMatcherTests()
		{
			this.methodData = new Dictionary<RpcPath, List<MethodInfo>>()
			{
				{
					nameof(MethodMatcherController), 
					typeof(MethodMatcherController)
						.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).ToList()
				},
				{
					nameof(MethodMatcherDuplicatesController),
					typeof(MethodMatcherDuplicatesController)
						.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).ToList()
				}
			};
		}
		
		private StaticRpcMethodDataAccessor GetMethodDataAccessor()
		{
			return new StaticRpcMethodDataAccessor()
			{
				Value = new StaticRpcMethodData(new List<MethodInfo>(), this.methodData)
			};
		}
		
		private DefaultRequestMatcher GetMatcher()
		{
			
			var logger = new Mock<ILogger<DefaultRequestMatcher>>(MockBehavior.Loose);
			this.rpcContext = new Mock<IRpcContext>(MockBehavior.Strict);
			var rpcContextAccessor = new Mock<IRpcContextAccessor>(MockBehavior.Strict);

			this.rpcContext.Setup(x => x.Path).Returns(typeof(MethodMatcherController).GetTypeInfo().Name);
			rpcContextAccessor.Setup(p => p.Value).Returns(this.rpcContext.Object);
			
			var methodProvider = new StaticRpcMethodProvider(this.GetMethodDataAccessor(), rpcContextAccessor.Object);
			return new DefaultRequestMatcher(logger.Object, methodProvider);
		}

		[Fact]
		public void GetMatchingMethod_WithRpcRoute()
		{
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);

			DefaultRequestMatcher matcher = this.GetMatcher();
			var requestSignature = RpcRequestSignature.Create(nameof(MethodMatcherController), methodName, new[] { RpcParameterType.String });

			RpcMethodInfo methodInfoMatched = matcher.GetMatchingMethod(requestSignature);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo.DeclaringType, methodInfoMatched.MethodInfo.DeclaringType);
			
			
			requestSignature = RpcRequestSignature.Create(nameof(MethodMatcherDuplicatesController), methodName, new[] { RpcParameterType.String });
			this.rpcContext.Setup(x => x.Path).Returns(typeof(MethodMatcherDuplicatesController).GetTypeInfo().Name);
			methodInfoMatched = matcher.GetMatchingMethod(requestSignature);
			expectedMethodInfo = typeof(MethodMatcherDuplicatesController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo.DeclaringType, methodInfoMatched.MethodInfo.DeclaringType); //method from cache with similar method name and params
		}
		
		[Fact]
		public void GetMatchingMethod_GuidParameter_Match()
		{
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);

			DefaultRequestMatcher matcher = this.GetMatcher();
			var requestSignature = RpcRequestSignature.Create("TestRoute", methodName, new[] { RpcParameterType.String });
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
			var requestSignature = RpcRequestSignature.Create("TestRoute", methodName, parameters);
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
			var requestSignature = RpcRequestSignature.Create("TestRoute", methodName, parameters);
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
		public void GetMatchingMethod_ListParam_Match()
		{
			DefaultRequestMatcher matcher = this.GetMatcher();

			RpcParameterType[] parameters = new[] { RpcParameterType.Object };
			string methodName = nameof(MethodMatcherController.List);
			var requestSignature = RpcRequestSignature.Create("TestRoute", methodName, parameters);
			RpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo, methodInfo.MethodInfo);
			Assert.Single(methodInfo.Parameters);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(List<string>), methodInfo.Parameters[0].RawType);
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[0].Type);
			Assert.Equal("values", methodInfo.Parameters[0].Name);
		}

		[Fact]
		public void GetMatchingMethod_CulturallyInvariantComparison()
		{
			DefaultRequestMatcher matcher = this.GetMatcher();

			RpcParameterType[] parameters = Array.Empty<RpcParameterType>();
			string methodName = nameof(MethodMatcherController.IsLunchTime);
			// Use lowercase version of method name when making request.
			var methodNameLower = methodName.ToLowerInvariant();
			var requestSignature = RpcRequestSignature.Create("TestRoute", methodNameLower, parameters);
			var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
			// Switch to a culture that would result in lowercasing 'I' to
			// U+0131, if not done with invariant culture.
			System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("az");
			RpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);

			Assert.NotNull(methodInfo);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo, methodInfo.MethodInfo);
			System.Globalization.CultureInfo.CurrentCulture = previousCulture;
		}
		
		[Theory]
		[InlineData("parameterOne")]
		[InlineData("parameter_one")]
		[InlineData("PARAMETER_ONE")]
		[InlineData("parameter-one")]
		[InlineData("PARAMETER-ONE")]
		public void GetMatchingMethod_ListParam_Match_Snake_Case(string parameterNameCase)
		{
			DefaultRequestMatcher matcher = this.GetMatcher();

			IEnumerable<KeyValuePair<string, RpcParameterType>> parameters = new[]
			{
				new KeyValuePair<string, RpcParameterType>(parameterNameCase, RpcParameterType.String)
			};
			
			string methodName = nameof(MethodMatcherController.SnakeCaseParams);
			var requestSignature = RpcRequestSignature.Create("TestRoute", methodName, parameters);
			RpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			MethodInfo expectedMethodInfo = typeof(MethodMatcherController).GetMethod(methodName)!;
			Assert.Equal(expectedMethodInfo, methodInfo.MethodInfo);
			Assert.Single(methodInfo.Parameters);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(string), methodInfo.Parameters[0].RawType);
			Assert.Equal(RpcParameterType.String, methodInfo.Parameters[0].Type);
			Assert.True(RpcUtil.NamesMatch(methodInfo.Parameters[0].Name, parameterNameCase));
		}
	}

}
