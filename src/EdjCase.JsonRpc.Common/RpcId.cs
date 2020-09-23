using EdjCase.JsonRpc.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Common
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

		public object? Value { get; }

		public long NumberValue
		{
			get
			{
				if (this.Type != RpcIdType.Number)
				{
					throw new InvalidOperationException("Cannot cast id to number.");
				}
				return (long)this.Value!;
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
				return (string)this.Value!;
			}
		}

		public RpcId(string id)
		{
			this.HasValue = true;
			this.Value = id;
			this.Type = RpcIdType.String;
		}

		public RpcId(long id)
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
			return this.Type switch
			{
				RpcIdType.Number => this.NumberValue == other.NumberValue,
				RpcIdType.String => this.StringValue == other.StringValue,
				_ => throw new ArgumentOutOfRangeException(nameof(this.Type)),
			};
		}

		public override bool Equals(object? obj)
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
			return this.Value!.GetHashCode();
		}

		public override string? ToString()
		{
			if (!this.HasValue)
			{
				return string.Empty;
			}
			return this.Type switch
			{
				RpcIdType.Number => this.Value!.ToString(),
				RpcIdType.String => "'" + (string)this.Value! + "'",
				_ => throw new ArgumentOutOfRangeException(nameof(this.Type)),
			};
		}

		public static implicit operator RpcId(long id)
		{
			return new RpcId(id);
		}

		public static RpcId FromInt64(long id)
		{
			return new RpcId(id);
		}

		public static implicit operator RpcId(string id)
		{
			return new RpcId(id);
		}

		public static RpcId FromString(string id)
		{
			return new RpcId(id);
		}

		public static RpcId FromObject(object value)
		{
			if (value == null)
			{
				return default;
			}
			if (value is RpcId rpcId)
			{
				return rpcId;
			}
			if (value is string stringValue)
			{
				return new RpcId(stringValue);
			}
			if (value.GetType().IsNumericType())
			{
				return new RpcId(Convert.ToInt64(value));
			}
			throw new RpcException(RpcErrorCode.InvalidRequest, "Id must be a string, a number or null.");
		}
	}
}
