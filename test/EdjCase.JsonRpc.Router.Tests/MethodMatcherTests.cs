﻿using EdjCase.JsonRpc.Router.Abstractions;
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
		private readonly IReadOnlyDictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>> methodData;

		public MethodMatcherTests()
		{
			this.methodData = new Dictionary<RpcPath, IReadOnlyList<IRpcMethodInfo>>
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
		}

		private StaticRpcMethodDataAccessor GetMethodDataAccessor()
		{
			return new StaticRpcMethodDataAccessor()
			{
				Value = new RpcRouteMetaData(new List<IRpcMethodInfo>(), this.methodData)
			};
		}

		private DefaultRequestMatcher GetMatcher(RpcPath? path = null)
		{

			var logger = new Mock<ILogger<DefaultRequestMatcher>>(MockBehavior.Loose);
			var rpcContextAccessor = new Mock<IRpcContextAccessor>(MockBehavior.Strict);

			rpcContextAccessor
			.Setup(p => p.Get())
			.Returns(new RpcContext(null, path));


			var methodProvider = new StaticRpcMethodProvider(this.GetMethodDataAccessor());
			return new DefaultRequestMatcher(logger.Object, methodProvider, rpcContextAccessor.Object);
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
			Assert.Equal(RpcParameterType.Object, methodInfo.Parameters[0].Type);
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
			DefaultRequestMatcher matcher = this.GetMatcher(path: typeof(MethodMatcherController).GetTypeInfo().Name);

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
			Assert.Equal(RpcParameterType.String, methodInfo.Parameters[0].Type);
			Assert.True(RpcUtil.NamesMatch(methodInfo.Parameters[0].Name, parameterNameCase));
		}
	}

}
