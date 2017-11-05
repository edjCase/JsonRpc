using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Core
{
	public struct RpcId : IEquatable<RpcId>
	{
		public bool HasValue { get; }

		public bool IsString { get; }

		public bool IsNumber { get; }

		public object Value { get; }

		public double NumberValue
		{
			get
			{
				if (!this.IsNumber)
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
				if (!this.IsString)
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
			this.IsString = true;
			this.IsNumber = false;
		}

		public RpcId(double id)
		{
			this.HasValue = true;
			this.Value = id;
			this.IsNumber = true;
			this.IsString = false;
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
			if(this.HasValue && other.HasValue)
			{
				return true;
			}
			if(this.HasValue || other.HasValue)
			{
				return false;
			}
			if(this.IsNumber && other.IsNumber)
			{
				return this.NumberValue == other.NumberValue;
			}
			return this.StringValue == other.StringValue;
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
	}
}
