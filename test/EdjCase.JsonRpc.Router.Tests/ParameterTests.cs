using EdjCase.JsonRpc.Router;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class ParameterTests
	{
		private JsonBytesRpcParameter BuildParam(string json)
		{
			var bytes = new Memory<byte>(Encoding.UTF8.GetBytes(json));
			return new JsonBytesRpcParameter(RpcParameterType.Object, bytes);
		}

		[Fact]
		public void TryGetValue_String_StringParsed()
		{
			const string expected = "Test";
			JsonBytesRpcParameter param = this.BuildParam($"\"{expected}\"");
			bool parsed = param.TryGetValue(out string actual);
			Assert.True(parsed);
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void TryGetValue_Number_NumberParsed()
		{
			JsonBytesRpcParameter param = this.BuildParam("1");

			bool parsed = param.TryGetValue(out int actual);
			Assert.True(parsed);
			Assert.Equal(1, actual);

			parsed = param.TryGetValue(out long actual2);
			Assert.True(parsed);
			Assert.Equal(1L, actual2);

			parsed = param.TryGetValue(out short actual3);
			Assert.True(parsed);
			Assert.Equal(1, actual3);

			parsed = param.TryGetValue(out float actual4);
			Assert.True(parsed);
			Assert.Equal(1f, actual4);

			parsed = param.TryGetValue(out double actual5);
			Assert.True(parsed);
			Assert.Equal(1d, actual5);

			parsed = param.TryGetValue(out decimal actual6);
			Assert.True(parsed);
			Assert.Equal(1m, actual6);
		}

		[Fact]
		public void TryGetValue_DateTime_DateTimeParsed()
		{
			const string expected = "2019-01-01T00:00:00.000";
			JsonBytesRpcParameter param = this.BuildParam($"\"{expected}\"");

			bool parsed = param.TryGetValue(out DateTime actual);
			Assert.True(parsed);
			Assert.Equal(DateTime.Parse(expected), actual);

			parsed = param.TryGetValue(out DateTimeOffset actual2);
			Assert.True(parsed);
			Assert.Equal(DateTimeOffset.Parse(expected), actual2);

			parsed = param.TryGetValue(out string actual3);
			Assert.True(parsed);
			Assert.Equal(expected, actual3);
		}

		[Fact]
		public void TryGetValue_Object_ObjectParsed()
		{
			const string expectedDateTime = "2019-01-01T00:00:00.000";
			const decimal expectedDecimal = 1.1m;
			const int expectedInteger = 1;
			const string expectedString = "Test";
			string json = $"{{\"DateTime\": \"{expectedDateTime}\", \"String\": \"{expectedString}\", \"Integer\": {expectedInteger}, \"Decimal\": {expectedDecimal}}}";

			JsonBytesRpcParameter param = this.BuildParam(json);

			bool parsed = param.TryGetValue(out TestObject actual);
			Assert.True(parsed);
			Assert.Equal(DateTime.Parse(expectedDateTime), actual.DateTime);
			Assert.Equal(expectedDecimal, actual.Decimal);
			Assert.Equal(expectedInteger, actual.Integer);
			Assert.Equal(expectedString, actual.String);
		}

		private class TestObject
		{
			public string String { get; set; }
			public DateTime DateTime { get; set; }
			public int Integer { get; set; }
			public decimal Decimal { get; set; }
		}

		[Fact]
		public void TryGetValue_Array_ArrayParsed()
		{
			string json = $"[0,1,2,3,4]";
			JsonBytesRpcParameter param = this.BuildParam(json);

			bool parsed = param.TryGetValue(out List<int> actual);
			Assert.True(parsed);
			for (int i = 0; i < actual.Count; i++)
			{
				Assert.Equal(i, actual[i]);
			}
		}
	}
}
