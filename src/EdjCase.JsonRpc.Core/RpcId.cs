using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Core
{
	public enum RpcIdType
	{
		String,
		Number
	}

	public struct RpcId : IEquatable<RpcId>
	{
		public bool HasValue { get; }

		public RpcIdType Type { get; }

		public object Value { get; }

		public double NumberValue
		{
			get
			{
				if (this.Type != RpcIdType.Number)
				{
					throw new InvalidOperationException("Cannot cast id to number.");
				}
				return (double)this.Value;
			}
		}

		public string StringValue
		{
			get
			{
				if (this.Type != RpcIdType.String)
				{
					throw new InvalidOperationException("Cannot cast id to string.");
				}
				return (string)this.Value;
			}
		}

		public RpcId(string id)
		{
			this.HasValue = true;
			this.Value = id;
			this.Type = RpcIdType.String;
		}

		public RpcId(double id)
		{
			this.HasValue = true;
			this.Value = id;
			this.Type = RpcIdType.Number;
		}

		public static bool operator ==(RpcId x, RpcId y)
		{
			return x.Equals(y);
		}

		public static bool operator !=(RpcId x, RpcId y)
		{
			return !x.Equals(y);
		}

		public bool Equals(RpcId other)
		{
			if (this.HasValue && other.HasValue)
			{
				return true;
			}
			if (this.HasValue || other.HasValue)
			{
				return false;
			}
			if (this.Type != other.Type)
			{
				return false;
			}
			switch (this.Type)
			{
				case RpcIdType.Number:
					return this.NumberValue == other.NumberValue;
				case RpcIdType.String:
					return this.StringValue == other.StringValue;
				default:
					throw new ArgumentOutOfRangeException(nameof(this.Type));
			}

		}

		public override bool Equals(object obj)
		{
			if (obj is RpcId id)
			{
				return this.Equals(id);
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (!this.HasValue)
			{
				return 0;
			}
			return this.Value.GetHashCode();
		}

		public static implicit operator RpcId(double id)
		{
			return new RpcId(id);
		}

		public static implicit operator RpcId(string id)
		{
			return new RpcId(id);
		}
	}
}
