using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Defaults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class RpcParameterConverterTests
	{
		private readonly Mock<IOptions<RpcServerConfiguration>> options;
		private readonly Mock<ILogger<DefaultRpcParameterConverter>> logger;
		public RpcParameterConverterTests()
		{
			this.options = new Mock<IOptions<RpcServerConfiguration>>();
			this.logger = new Mock<ILogger<DefaultRpcParameterConverter>>();
		}

		//TODO
		//[Fact]
		//public void TryGetValue_String_StringParsed()
		//{
		//	const string expected = "Test";
		//	RpcParameter param = RpcParameter.String(expected);
		//	var converter = new DefaultRpcParameterConverter(this.options.Object, this.logger.Object);
		//	bool parsed = converter.TryConvertValue(param, RpcParameterType.String, typeof(string), out object? actual);
		//	Assert.True(parsed);
		//	Assert.Equal(expected, actual);
		//}

		//[Fact]
		//public void TryGetValue_Number_NumberParsed()
		//{
		//	RpcParameter param = RpcParameter.Number;

		//	bool parsed = param.TryGetValue(out int? actual);
		//	Assert.True(parsed);
		//	Assert.Equal(1, actual);

		//	parsed = param.TryGetValue(out long actual2);
		//	Assert.True(parsed);
		//	Assert.Equal(1L, actual2);

		//	parsed = param.TryGetValue(out short actual3);
		//	Assert.True(parsed);
		//	Assert.Equal(1, actual3);

		//	parsed = param.TryGetValue(out float actual4);
		//	Assert.True(parsed);
		//	Assert.Equal(1f, actual4);

		//	parsed = param.TryGetValue(out double actual5);
		//	Assert.True(parsed);
		//	Assert.Equal(1d, actual5);

		//	parsed = param.TryGetValue(out decimal actual6);
		//	Assert.True(parsed);
		//	Assert.Equal(1m, actual6);
		//}

		//[Fact]
		//public void TryGetValue_DateTime_DateTimeParsed()
		//{
		//	const string expected = "2019-01-01T00:00:00.000";
		//	JsonBytesRpcParameter param = this.BuildParam($"\"{expected}\"");

		//	bool parsed = param.TryGetValue(out DateTime actual);
		//	Assert.True(parsed);
		//	Assert.Equal(DateTime.Parse(expected), actual);

		//	parsed = param.TryGetValue(out DateTimeOffset actual2);
		//	Assert.True(parsed);
		//	Assert.Equal(DateTimeOffset.Parse(expected), actual2);

		//	parsed = param.TryGetValue(out string actual3);
		//	Assert.True(parsed);
		//	Assert.Equal(expected, actual3);
		//}

		//[Fact]
		//public void TryGetValue_Object_ObjectParsed()
		//{
		//	const string expectedDateTime = "2019-01-01T00:00:00.000";
		//	const decimal expectedDecimal = 1.1m;
		//	const int expectedInteger = 1;
		//	const string expectedString = "Test";
		//	string json = $"{{\"DateTime\": \"{expectedDateTime}\", \"String\": \"{expectedString}\", \"Integer\": {expectedInteger}, \"Decimal\": {expectedDecimal}}}";

		//	JsonBytesRpcParameter param = this.BuildParam(json);

		//	bool parsed = param.TryGetValue(out TestObject actual);
		//	Assert.True(parsed);
		//	Assert.Equal(DateTime.Parse(expectedDateTime), actual.DateTime);
		//	Assert.Equal(expectedDecimal, actual.Decimal);
		//	Assert.Equal(expectedInteger, actual.Integer);
		//	Assert.Equal(expectedString, actual.String);
		//}

		//private class TestObject
		//{
		//	public string? String { get; set; }
		//	public DateTime DateTime { get; set; }
		//	public int Integer { get; set; }
		//	public decimal Decimal { get; set; }
		//}

		//[Fact]
		//public void TryGetValue_Array_ArrayParsed()
		//{
		//	string json = $"[0,1,2,3,4]";
		//	JsonBytesRpcParameter param = this.BuildParam(json);

		//	bool parsed = param.TryGetValue(out List<int> actual);
		//	Assert.True(parsed);
		//	for (int i = 0; i < actual.Count; i++)
		//	{
		//		Assert.Equal(i, actual[i]);
		//	}
		//}
	}
}
