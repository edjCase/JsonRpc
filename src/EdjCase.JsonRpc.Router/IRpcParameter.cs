using EdjCase.JsonRpc.Router.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router
{
	public class RpcParameter
	{
		public RpcParameterType Type { get; }
		private object? value { get; }

		private RpcParameter(RpcParameterType type, object? value)
		{
			this.Type = type;
			this.value = value;
		}

		public bool GetBooleanValue()
		{
			this.ThrowIfNoType(RpcParameterType.Boolean);
			return (bool)this.value!;
		}
		public RpcNumber GetNumberValue()
		{
			this.ThrowIfNoType(RpcParameterType.Number);
			return (RpcNumber)this.value!;
		}
		public string GetStringValue()
		{
			this.ThrowIfNoType(RpcParameterType.String);
			return (string)this.value!;
		}

		public Dictionary<string, RpcParameter> GetObjectValue()
		{
			this.ThrowIfNoType(RpcParameterType.Object);
			return (Dictionary<string, RpcParameter>)this.value!;
		}

		public RpcParameter[] GetArrayValue()
		{
			this.ThrowIfNoType(RpcParameterType.Array);
			return (RpcParameter[])this.value!;
		}

		private void ThrowIfNoType(RpcParameterType type)
		{
			if (this.Type != type)
			{
				throw new InvalidOperationException($"Cannot get a {type} value from a {this.Type} value");
			}
		}

		public static RpcParameter String(string value)
		{
			return new RpcParameter(RpcParameterType.String, value);
		}

		public static RpcParameter Number(RpcNumber value)
		{
			return new RpcParameter(RpcParameterType.Number, value);
		}

		public static RpcParameter Boolean(bool value)
		{
			return new RpcParameter(RpcParameterType.Boolean, value);
		}

		public static RpcParameter Null()
		{
			return new RpcParameter(RpcParameterType.Null, null);
		}

		public static RpcParameter Object(Dictionary<string, RpcParameter> value)
		{
			return new RpcParameter(RpcParameterType.Object, value);
		}

		public static RpcParameter Array(RpcParameter[] value)
		{
			return new RpcParameter(RpcParameterType.Array, value);
		}
	}

	public enum RpcParameterType
	{
		Null,
		Boolean,
		Number,
		String,
		Object,
		Array
	}

}
