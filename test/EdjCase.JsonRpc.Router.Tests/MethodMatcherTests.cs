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
using Microsoft.Extensions.Options;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class MethodMatcherTests
	{
		private readonly StaticRpcMethodDataAccessor methodDataAccessor;

		public MethodMatcherTests()
		{
			var baserouteData = typeof(MethodMatcherThreeController)
						.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
						.Select(DefaultRpcMethodInfo.FromMethodInfo)
						.ToList();

			var routeData = new Dictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>>
			{
				[nameof(MethodMatcherController)] = typeof(MethodMatcherController)
						.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
						.Select(DefaultRpcMethodInfo.FromMethodInfo)
						.ToList(),
				[nameof(MethodMatcherDuplicatesController)] = typeof(MethodMatcherDuplicatesController)
						.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
						.Select(DefaultRpcMethodInfo.FromMethodInfo)
						.ToList()
			};
			this.methodDataAccessor = new StaticRpcMethodDataAccessor() { Value = new RpcRouteMetaData(baserouteData, routeData)};
		}

 
		private DefaultRequestMatcher GetMatcher(RpcPath? path = null)
		{

			var logger = new Mock<ILogger<DefaultRequestMatcher>>(MockBehavior.Loose);
			var rpcContextAccessor = new Mock<IRpcContextAccessor>(MockBehavior.Strict);
			var options = new Mock<IOptions<RpcServerConfiguration>>();
			options.Setup(o => o.Value).Returns(new RpcServerConfiguration
			{
				JsonSerializerSettings = null
			});
			var logger2 = new Mock<ILogger<DefaultRpcParameterConverter>>();
			var rpcParameterConverter = new DefaultRpcParameterConverter(options.Object, logger2.Object);

			rpcContextAccessor
			.Setup(p => p.Get())
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			.Returns(new RpcContext(null, path));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.


			var methodProvider = new StaticRpcMethodProvider(this.methodDataAccessor);
			return new DefaultRequestMatcher(
				logger.Object,
				methodProvider,
				rpcContextAccessor.Object,
				rpcParameterConverter,
				new RequestMatcherCache(Options.Create(new RequestCacheOptions()))
			);
		}

		[Fact]
		public void GetMatchingMethod_WithRpcRoute()
		{
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);
			RpcRequestSignature requestSignature = RpcRequestSignature.Create(methodName, new[] { RpcParameterType.String });

			DefaultRequestMatcher path1Matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);
			IRpcMethodInfo path1Match = path1Matcher.GetMatchingMethod(requestSignature);
			Assert.NotNull(path1Match);


			DefaultRequestMatcher path2Matcher = this.GetMatcher(path: typeof(MethodMatcherDuplicatesController).GetTypeInfo().Name);
			IRpcMethodInfo path2Match = path2Matcher.GetMatchingMethod(requestSignature);
			Assert.NotNull(path2Match);
			Assert.NotSame(path1Match, path2Match);

			DefaultRequestMatcher path3Matcher = this.GetMatcher(path: null);
			IRpcMethodInfo path3Match = path3Matcher.GetMatchingMethod(requestSignature);
			Assert.NotNull(path2Match);
		}

		[Fact]
		public void GetMatchingMethod_GuidParameter_Match()
		{
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);

			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);
			var requestSignature = RpcRequestSignature.Create(methodName, new[] { RpcParameterType.String });
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);

			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			Assert.Single(methodInfo.Parameters);
			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(Guid), methodInfo.Parameters[0].RawType);
			Assert.Equal("guid", methodInfo.Parameters[0].Name);
		}




		[Fact]
		public void GetMatchingMethod_SimpleMulitParam_DictMatch()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);

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
			Assert.Equal("a", methodInfo.Parameters[0].Name);

			Assert.False(methodInfo.Parameters[1].IsOptional);
			Assert.Equal(typeof(bool), methodInfo.Parameters[1].RawType);
			Assert.Equal("b", methodInfo.Parameters[1].Name);

			Assert.False(methodInfo.Parameters[2].IsOptional);
			Assert.Equal(typeof(string), methodInfo.Parameters[2].RawType);
			Assert.Equal("c", methodInfo.Parameters[2].Name);

			Assert.False(methodInfo.Parameters[3].IsOptional);
			Assert.Equal(typeof(object), methodInfo.Parameters[3].RawType);
			Assert.Equal("d", methodInfo.Parameters[3].Name);

			Assert.True(methodInfo.Parameters[4].IsOptional);
			Assert.Equal(typeof(int?), methodInfo.Parameters[4].RawType);
			Assert.Equal("e", methodInfo.Parameters[4].Name);
		}
		[Fact]
		public void GetMatchingMethod_SimpleMulitParam_ListMatch()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);

			RpcParameterType[] parameters = new[] { RpcParameterType.Number, RpcParameterType.Boolean, RpcParameterType.String, RpcParameterType.Object, RpcParameterType.Null };
			string methodName = nameof(MethodMatcherController.SimpleMulitParam);
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			Assert.Equal(5, methodInfo.Parameters.Count);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(int), methodInfo.Parameters[0].RawType);
			Assert.Equal("a", methodInfo.Parameters[0].Name);

			Assert.False(methodInfo.Parameters[1].IsOptional);
			Assert.Equal(typeof(bool), methodInfo.Parameters[1].RawType);
			Assert.Equal("b", methodInfo.Parameters[1].Name);

			Assert.False(methodInfo.Parameters[2].IsOptional);
			Assert.Equal(typeof(string), methodInfo.Parameters[2].RawType);
			Assert.Equal("c", methodInfo.Parameters[2].Name);

			Assert.False(methodInfo.Parameters[3].IsOptional);
			Assert.Equal(typeof(object), methodInfo.Parameters[3].RawType);
			Assert.Equal("d", methodInfo.Parameters[3].Name);

			Assert.True(methodInfo.Parameters[4].IsOptional);
			Assert.Equal(typeof(int?), methodInfo.Parameters[4].RawType);
			Assert.Equal("e", methodInfo.Parameters[4].Name);
		}
		[Fact]
		public void GetMatchingMethod_ListParam_Match()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);

			RpcParameterType[] parameters = new[] { RpcParameterType.Array };
			string methodName = nameof(MethodMatcherController.List);
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
			Assert.Single(methodInfo.Parameters);

			Assert.False(methodInfo.Parameters[0].IsOptional);
			Assert.Equal(typeof(List<string>), methodInfo.Parameters[0].RawType);
			Assert.Equal("values", methodInfo.Parameters[0].Name);
		}

		[Fact]
		public void GetMatchingMethod_CulturallyInvariantComparison()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);

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

		[Theory]
		[InlineData("parameterOne")]
		[InlineData("parameter_one")]
		[InlineData("PARAMETER_ONE")]
		[InlineData("parameter-one")]
		[InlineData("PARAMETER-ONE")]
		public void GetMatchingMethod_ListParam_Match_Snake_Case(string parameterNameCase)
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);

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
			Assert.True(RpcUtil.NamesMatch(methodInfo.Parameters[0].Name, parameterNameCase));
		}

		[Fact]
		public void GetMatchingMethod_Optional_NullListParam__Valid()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);
			string methodName = nameof(MethodMatcherController.Optional);

			RpcParameterType[] parameters = new[] { RpcParameterType.String, RpcParameterType.Null };
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);
			Validate(methodInfo);


			RpcParameterType[] parameters2 = new[] { RpcParameterType.String, RpcParameterType.String };
			var requestSignature2 = RpcRequestSignature.Create(methodName, parameters2);
			IRpcMethodInfo methodInfo2 = matcher.GetMatchingMethod(requestSignature2);
			Validate(methodInfo2);

			void Validate(IRpcMethodInfo methodInfo)
			{
				Assert.NotNull(methodInfo);
				Assert.Equal(methodName, methodInfo.Name);
				Assert.Equal(2, methodInfo.Parameters.Count);
				Assert.False(methodInfo.Parameters[0].IsOptional);
				Assert.True(methodInfo.Parameters[1].IsOptional);
				Assert.Equal(typeof(string), methodInfo.Parameters[0].RawType);
				Assert.Equal(typeof(string), methodInfo.Parameters[1].RawType);
				Assert.Equal("required", methodInfo.Parameters[0].Name);
				Assert.Equal("optional", methodInfo.Parameters[1].Name);
			}
		}

		[Fact]
		public void GetMatchingMethod_Optional_NullDictParam__Valid()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);
			string methodName = nameof(MethodMatcherController.Optional);

			var parameters = new Dictionary<string, RpcParameterType>
			{
				{ "required", RpcParameterType.String },
				{ "optional", RpcParameterType.Null }
			};
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);
			Validate(methodInfo);


			var parameters2 = new Dictionary<string, RpcParameterType>
			{
				{ "required", RpcParameterType.String },
				{ "optional", RpcParameterType.String }
			};
			var requestSignature2 = RpcRequestSignature.Create(methodName, parameters2);
			IRpcMethodInfo methodInfo2 = matcher.GetMatchingMethod(requestSignature2);
			Validate(methodInfo2);

			void Validate(IRpcMethodInfo methodInfo)
			{
				Assert.NotNull(methodInfo);
				Assert.Equal(methodName, methodInfo.Name);
				Assert.Equal(2, methodInfo.Parameters.Count);
				Assert.False(methodInfo.Parameters[0].IsOptional);
				Assert.True(methodInfo.Parameters[1].IsOptional);
				Assert.Equal(typeof(string), methodInfo.Parameters[0].RawType);
				Assert.Equal(typeof(string), methodInfo.Parameters[1].RawType);
				Assert.Equal("required", methodInfo.Parameters[0].Name);
				Assert.Equal("optional", methodInfo.Parameters[1].Name);
			}
		}

		// https://github.com/edjCase/JsonRpc/issues/99
		[Fact]
		public void GetMatchingMethod__Dictionary_Request_With_Optional_Parameters__Matches()
		{
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);
			string methodName = nameof(MethodMatcherController.CreateInfoHelperItem);

			var parameters = new Dictionary<string, RpcParameterType>
			{
				{ "name", RpcParameterType.String },
				{ "language", RpcParameterType.String },
				{ "value", RpcParameterType.String },
				{ "description", RpcParameterType.String },
				{ "component", RpcParameterType.String },
				{ "locationIndex", RpcParameterType.String },
				{ "fontSize", RpcParameterType.Number },
				{ "bold", RpcParameterType.Boolean },
				{ "italic", RpcParameterType.Boolean },
				{ "strikeout", RpcParameterType.Boolean },
				{ "underline", RpcParameterType.Boolean },
			};
			var requestSignature = RpcRequestSignature.Create(methodName, parameters);
			IRpcMethodInfo methodInfo = matcher.GetMatchingMethod(requestSignature);


			Assert.NotNull(methodInfo);
			Assert.Equal(methodName, methodInfo.Name);
		}


		[Fact]
		public void GetMatchingMethod_WithoutRpcRoute()
		{
			string methodName = nameof(MethodMatcherController.GuidTypeMethod);
			RpcRequestSignature requestSignature = RpcRequestSignature.Create(methodName, new[] { RpcParameterType.String });
 
			DefaultRequestMatcher path3Matcher = this.GetMatcher(path: null);
			IRpcMethodInfo path3Match = path3Matcher.GetMatchingMethod(requestSignature);
			Assert.NotNull(path3Match);
		}
	}

}
