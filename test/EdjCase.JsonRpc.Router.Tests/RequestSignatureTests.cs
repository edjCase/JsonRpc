using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace EdjCase.JsonRpc.Router.Tests
{
	public class RequestSignatureTests
	{
		[Fact]
		public void Create_NullListParam_Valid()
		{
			string methodName = "Test";
			var signature = RpcRequestSignature.Create(methodName, parameters: (RpcParameterType[]?)null);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.False(signature.HasParameters);
			Assert.False(signature.IsDictionary);
			Assert.Empty(signature.ParametersAsList);
		}

		[Fact]
		public void Create_EmptyListParam_Valid()
		{
			string methodName = "Test";
			RpcParameterType[] parameters = new RpcParameterType[0];
			var signature = RpcRequestSignature.Create(methodName, parameters);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.False(signature.HasParameters);
			Assert.False(signature.IsDictionary);
			Assert.Equal(parameters, signature.ParametersAsList);
		}

		[Fact]
		public void Create_SingleListParam_Valid()
		{
			string methodName = "Test";
			RpcParameterType[] parameters = new[] { RpcParameterType.String };
			var signature = RpcRequestSignature.Create(methodName, parameters);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.True(signature.HasParameters);
			Assert.False(signature.IsDictionary);
			Assert.Equal(parameters, signature.ParametersAsList);
		}

		[Fact]
		public void Create_MultiListParam_Valid()
		{
			string methodName = "Test";
			RpcParameterType[] parameters = new[] { RpcParameterType.String, RpcParameterType.Boolean, RpcParameterType.Null, RpcParameterType.Number, RpcParameterType.Object };
			var signature = RpcRequestSignature.Create(methodName, parameters);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.True(signature.HasParameters);
			Assert.False(signature.IsDictionary);
			Assert.Equal(parameters, signature.ParametersAsList);
		}

		[Fact]
		public void Create_NullDictParam_Valid()
		{
			string methodName = "Test";
			var signature = RpcRequestSignature.Create(methodName, parameters: (IEnumerable<KeyValuePair<string, RpcParameterType>>?)null);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.False(signature.HasParameters);
			Assert.False(signature.IsDictionary);
			Assert.Empty(signature.ParametersAsList);
		}

		[Fact]
		public void Create_EmptyDictParam_Valid()
		{
			string methodName = "Test";
			var parameters = new Dictionary<string, RpcParameterType>();
			var signature = RpcRequestSignature.Create(methodName, parameters);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.False(signature.HasParameters);
			Assert.True(signature.IsDictionary);
			this.AssertDictsEqual(parameters, signature.ParametersAsDict);
		}

		[Fact]
		public void Create_SingleDictParam_Valid()
		{
			string methodName = "Test";
			var parameters = new Dictionary<string, RpcParameterType>
			{
				["Param1"] = RpcParameterType.String
			};
			var signature = RpcRequestSignature.Create(methodName, parameters);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.True(signature.HasParameters);
			Assert.True(signature.IsDictionary);
			this.AssertDictsEqual(parameters, signature.ParametersAsDict);
		}

		[Fact]
		public void Create_MultiDictParam_Valid()
		{
			string methodName = "Test";
			var parameters = new Dictionary<string, RpcParameterType>
			{
				["String"] = RpcParameterType.String,
				["Boolean"] = RpcParameterType.Boolean,
				["Null"] = RpcParameterType.Null,
				["Number"] = RpcParameterType.Number,
				["Object"] = RpcParameterType.Object,
			};
			var signature = RpcRequestSignature.Create(methodName, parameters);
			Assert.Equal(methodName, signature.GetMethodName().ToString());
			Assert.True(signature.HasParameters);
			Assert.True(signature.IsDictionary);
			this.AssertDictsEqual(parameters, signature.ParametersAsDict);
		}


		private void AssertDictsEqual(Dictionary<string, RpcParameterType> parameters, IEnumerable<(Memory<char>, RpcParameterType)> parametersAsDict)
		{
			int parameterCount = 0;
			foreach ((Memory<char> name, RpcParameterType type) in parametersAsDict)
			{
				parameterCount++;
				if (!parameters.TryGetValue(name.ToString(), out RpcParameterType otherType))
				{
					throw new Xunit.Sdk.EqualException(name.ToString(), null);
				}
				Assert.Equal(otherType, type);
			}
			Assert.Equal(parameters.Count, parameterCount);
		}
	}
}
