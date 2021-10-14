using System;

namespace EdjCase.JsonRpc.Router
{
	public class RpcNumber
	{
		private readonly string stringValue;
		public RpcNumber(string stringValue)
		{
			this.stringValue = stringValue ?? throw new ArgumentNullException(nameof(stringValue));
		}

		public override string ToString()
		{
			return this.stringValue;
		}

		public static implicit operator RpcNumber(short d) => new RpcNumber(d.ToString());
		public bool TryGetShort(out short v)
		{
			return short.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(ushort d) => new RpcNumber(d.ToString());
		public bool TryGetUnsignedShort(out ushort v)
		{
			return ushort.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(int d) => new RpcNumber(d.ToString());
		public bool TryGetInteger(out int v)
		{
			return int.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(uint d) => new RpcNumber(d.ToString());
		public bool TryGetUnsignedInteger(out uint v)
		{
			return uint.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(long d) => new RpcNumber(d.ToString());
		public bool TryGetLong(out long v)
		{
			return long.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(ulong d) => new RpcNumber(d.ToString());
		public bool TryGetUnsingedLong(out ulong v)
		{
			return ulong.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(float d) => new RpcNumber(d.ToString());
		public bool TryGetFloat(out float v)
		{
			return float.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(double d) => new RpcNumber(d.ToString());
		public bool TryGetDouble(out double v)
		{
			return double.TryParse(this.stringValue, out v);
		}
		public static implicit operator RpcNumber(decimal d) => new RpcNumber(d.ToString());
		public bool TryGetDecimal(out decimal v)
		{
			return decimal.TryParse(this.stringValue, out v);
		}
	}
}