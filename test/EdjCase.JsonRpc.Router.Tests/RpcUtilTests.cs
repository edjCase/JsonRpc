using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.Options;
using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Utilities;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class RpcUtilTests
	{
		[Theory]
		[InlineData("TestCase", "testcase")]
		[InlineData("TestCase", "test_case")]
		[InlineData("TestCase", "TEST_CASE")]
		[InlineData("TestCase", "test-case")]
		[InlineData("TestCase", "TEST-CASE")]
		[InlineData("test", "Test")]
		public async Task MatchMethodNamesTest(string methodInfo, string requestMethodName)
		{
			Assert.True(RpcUtil.NamesMatch(methodInfo, requestMethodName));
		}

		[Fact]
		public void MatchMethodNamesCulturallyInvariantTest()
		{
			var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
			// Switch to a locale that would result in lowercasing 'I' to
			// U+0131, if not done with invariant culture.
			System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("az");
			var methodInfo = "IsLunchTime";
			var requestMethodName = "isLunchtIme";
			Assert.True(RpcUtil.NamesMatch(methodInfo, requestMethodName));
			System.Globalization.CultureInfo.CurrentCulture = previousCulture;
		}
	}
}
