using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdjCase.JsonRpc.Core
{
	public enum RpcParametersType
	{
		Array,
		Dictionary
	}

	public struct RpcParameters
	{
		public bool HasValue { get; }

		public RpcParametersType Type { get; }

		public object Value { get; }

		public Dictionary<string, object> DictionaryValue
		{
			get
			{
				if (this.Type != RpcParametersType.Dictionary)
				{
					throw new InvalidOperationException("Cannot cast params to dictionary.");
				}
				return (Dictionary<string, object>)this.Value;
			}
		}

		public object[] ArrayValue
		{
			get
			{
				if (this.Type != RpcParametersType.Array)
				{
					throw new InvalidOperationException("Cannot cast params to array.");
				}
				return (object[])this.Value;
			}
		}

		public RpcParameters(object[] parameters)
		{
			this.HasValue = true;
			this.Type = RpcParametersType.Array;
			this.Value = parameters ?? new object[0];
		}

		public RpcParameters(Dictionary<string, object> parameters)
		{
			this.HasValue = true;
			this.Type = RpcParametersType.Dictionary;
			this.Value = parameters ?? new Dictionary<string, object>();
		}

		public static RpcParameters Empty => new RpcParameters(new object[0]);

		public static RpcParameters FromList(IEnumerable<object> parameters)
		{
			return new RpcParameters(parameters?.ToArray());
		}

		public static RpcParameters From(params object[] parameters)
		{
			return new RpcParameters(parameters);
		}

		public static RpcParameters FromDictionary(IDictionary<string, object> parameters)
		{
			return new RpcParameters(parameters?.ToDictionary(kv => kv.Key, kv => kv.Value));
		}

		public static implicit operator RpcParameters(List<object> parameters)
		{
			return new RpcParameters(parameters?.ToArray());
		}


		public static implicit operator RpcParameters(object[] parameters)
		{
			return new RpcParameters(parameters);
		}

		public static implicit operator RpcParameters(Dictionary<string, object> parameters)
		{
			return new RpcParameters(parameters);
		}
	}
}
