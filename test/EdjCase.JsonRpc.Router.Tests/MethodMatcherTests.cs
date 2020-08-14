using EdjCase.JsonRpc.Router.Abstractions;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Utilities;
using Xunit;
using System.Linq;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class MethodMatcherTests
	{
		private IReadOnlyList<IRpcMethodInfo> methods;
		public MethodMatcherTests()
		{
			this.methods = typeof(MethodMatcherController)
				.GetMethods()
				.Select(DefaultRpcMethodInfo.FromMethodInfo)
				.ToList();
		}
		private DefaultRequestMatcher GetMatcher()
		{
			var logger = new Mock<ILogger<DefaultRequestMatcher>>(MockBehavior.Loose);
			var methodProvider = new Mock<IRpcMethodProvider>(MockBehavior.Strict);
			methodProvider
				.Setup(p => p.GetByPath(null))
				.Returns(this.methods);

			var contextAccessor = new Mock<IRpcContextAccessor>();
			contextAccessor
			.Setup(a => a.Get())
			.Returns(new RpcContext(null, null));

			return new DefaultRequestMatcher(logger.Object, methodProvider.Object, contextAccessor.Object);
		}


		[Fact]
		public void GetMatchingMethod_GuidParameter_Match()
		{
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);

			DefaultRequestMatcher matcher = this.GetMatcher();
			var requestSignature = RpcRequestSignature.Create(methodName, new[] { RpcParameterType.String });
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);

			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
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
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			Assert.Equal(5, methodInfo.Parameters.Count);

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
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			Assert.Equal(5, methodInfo.Parameters.Count);

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
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
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
			var requestSignature = RpcRequestSignature.Create(methodNameLower, parameters);
			var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
			// Switch to a culture that would result in lowercasing 'I' to
			// U+0131, if not done with invariant culture.
			System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("az");
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);

			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			System.Globalization.CultureInfo.CurrentCulture = previousCulture;
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

			public List<string> List(List<string> values)
			{
				return values;
			}

			public string SnakeCaseParams(string parameterOne)
			{
				return parameterOne;
			}

			public bool IsLunchTime()
			{
				return true;
			}
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
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			Assert.Single(methodInfo.Parameters);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(string), methodInfo.Parameters[0].RawType);
			Assert.Equal(RpcParameterType.String, methodInfo.Parameters[0].Type);
			Assert.True(RpcUtil.NamesMatch(methodInfo.Parameters[0].Name, parameterNameCase));
		}
	}

}
