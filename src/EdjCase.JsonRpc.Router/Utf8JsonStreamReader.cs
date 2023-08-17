using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router
{
	public ref struct Utf8JsonStreamReader
	{
		private readonly Stream _stream;
		private readonly int _bufferSize;

		private SequenceSegment? _firstSegment;
		private int _firstSegmentStartIndex;
		private SequenceSegment? _lastSegment;
		private int _lastSegmentEndIndex;

		private Utf8JsonReader _jsonReader;
		private bool _keepBuffers;
		private bool _isFinalBlock;

		public Utf8JsonStreamReader(Stream stream, int bufferSize = 32 * 1024)
		{
			this._stream = stream;
			this._bufferSize = bufferSize;

			this._firstSegment = null;
			this._firstSegmentStartIndex = 0;
			this._lastSegment = null;
			this._lastSegmentEndIndex = -1;

			this._jsonReader = default;
			this._keepBuffers = false;
			this._isFinalBlock = false;
		}

		public bool Read()
		{
			// read could be unsuccessful due to insufficient bufer size, retrying in loop with additional buffer segments
			while (!this._jsonReader.Read())
			{
				if (this._isFinalBlock)
					return false;

				this.MoveNext();
			}

			return true;
		}

		private void MoveNext()
		{
			var firstSegment = this._firstSegment;
			this._firstSegmentStartIndex += (int)this._jsonReader.BytesConsumed;

			// release previous segments if possible
			if (!this._keepBuffers)
			{
				while (firstSegment?.Memory.Length <= this._firstSegmentStartIndex)
				{
					this._firstSegmentStartIndex -= firstSegment.Memory.Length;
					firstSegment.Dispose();
					firstSegment = (SequenceSegment?)firstSegment.Next;
				}
			}

			// create new segment
			var newSegment = new SequenceSegment(this._bufferSize, this._lastSegment);

			if (firstSegment != null)
			{
				this._firstSegment = firstSegment;
				newSegment.Previous = this._lastSegment;
				this._lastSegment?.SetNext(newSegment);
				this._lastSegment = newSegment;
			}
			else
			{
				this._firstSegment = this._lastSegment = newSegment;
				this._firstSegmentStartIndex = 0;
			}

			// read data from stream
			this._lastSegmentEndIndex = this._stream.Read(newSegment.Buffer.Memory.Span);
			this._isFinalBlock = this._lastSegmentEndIndex < newSegment.Buffer.Memory.Length;
			this._jsonReader = new Utf8JsonReader(new ReadOnlySequence<byte>(this._firstSegment, this._firstSegmentStartIndex, this._lastSegment, this._lastSegmentEndIndex), this._isFinalBlock, this._jsonReader.CurrentState);
		}

		public T Deserialize<T>(JsonSerializerOptions? options = null)
		{
			// JsonSerializer.Deserialize can read only a single object. We have to extract
			// object to be deserialized into separate Utf8JsonReader. This incures one additional
			// pass through data (but data is only passed, not parsed).
			var tokenStartIndex = this._jsonReader.TokenStartIndex;
			var firstSegment = this._firstSegment;
			var firstSegmentStartIndex = this._firstSegmentStartIndex;

			// loop through data until end of object is found
			this._keepBuffers = true;
			int depth = 0;

			if (this.TokenType == JsonTokenType.StartObject || this.TokenType == JsonTokenType.StartArray)
				depth++;

			while (depth > 0 && this.Read())
			{
				if (this.TokenType == JsonTokenType.StartObject || this.TokenType == JsonTokenType.StartArray)
					depth++;
				else if (this.TokenType == JsonTokenType.EndObject || this.TokenType == JsonTokenType.EndArray)
					depth--;
			}

			this._keepBuffers = false;

			// end of object found, extract json reader for deserializer
			var newJsonReader = new Utf8JsonReader(new ReadOnlySequence<byte>(firstSegment!, firstSegmentStartIndex, this._lastSegment!, this._lastSegmentEndIndex).Slice(tokenStartIndex, this._jsonReader.Position), true, default);

			// deserialize value
			var result = JsonSerializer.Deserialize<T>(ref newJsonReader, options)!;

			// release memory if possible
			firstSegmentStartIndex = this._firstSegmentStartIndex + (int)this._jsonReader.BytesConsumed;

			while (firstSegment?.Memory.Length < firstSegmentStartIndex)
			{
				firstSegmentStartIndex -= firstSegment.Memory.Length;
				firstSegment.Dispose();
				firstSegment = (SequenceSegment?)firstSegment.Next;
			}

			if (firstSegment != this._firstSegment)
			{
				this._firstSegment = firstSegment;
				this._firstSegmentStartIndex = firstSegmentStartIndex;
				this._jsonReader = new Utf8JsonReader(new ReadOnlySequence<byte>(this._firstSegment!, this._firstSegmentStartIndex, this._lastSegment!, this._lastSegmentEndIndex), this._isFinalBlock, this._jsonReader.CurrentState);
			}

			return result;
		}

		public void Dispose() => this._lastSegment?.Dispose();

		public int CurrentDepth => this._jsonReader.CurrentDepth;
		public bool HasValueSequence => this._jsonReader.HasValueSequence;
		public long TokenStartIndex => this._jsonReader.TokenStartIndex;
		public JsonTokenType TokenType => this._jsonReader.TokenType;
		public ReadOnlySequence<byte> ValueSequence => this._jsonReader.ValueSequence;
		public ReadOnlySpan<byte> ValueSpan => this._jsonReader.ValueSpan;

		public bool GetBoolean() => this._jsonReader.GetBoolean();
		public byte GetByte() => this._jsonReader.GetByte();
		public byte[] GetBytesFromBase64() => this._jsonReader.GetBytesFromBase64();
		public string GetComment() => this._jsonReader.GetComment();
		public DateTime GetDateTime() => this._jsonReader.GetDateTime();
		public DateTimeOffset GetDateTimeOffset() => this._jsonReader.GetDateTimeOffset();
		public decimal GetDecimal() => this._jsonReader.GetDecimal();
		public double GetDouble() => this._jsonReader.GetDouble();
		public Guid GetGuid() => this._jsonReader.GetGuid();
		public short GetInt16() => this._jsonReader.GetInt16();
		public int GetInt32() => this._jsonReader.GetInt32();
		public long GetInt64() => this._jsonReader.GetInt64();
		public sbyte GetSByte() => this._jsonReader.GetSByte();
		public float GetSingle() => this._jsonReader.GetSingle();
		public string GetString() => this._jsonReader.GetString() ?? throw new InvalidOperationException("Value is not a string");
		public uint GetUInt32() => this._jsonReader.GetUInt32();
		public ulong GetUInt64() => this._jsonReader.GetUInt64();
		public bool TryGetDecimal(out byte value) => this._jsonReader.TryGetByte(out value);
		public bool TryGetBytesFromBase64(out byte[] value) => this._jsonReader.TryGetBytesFromBase64(out value!);
		public bool TryGetDateTime(out DateTime value) => this._jsonReader.TryGetDateTime(out value);
		public bool TryGetDateTimeOffset(out DateTimeOffset value) => this._jsonReader.TryGetDateTimeOffset(out value);
		public bool TryGetDecimal(out decimal value) => this._jsonReader.TryGetDecimal(out value);
		public bool TryGetDouble(out double value) => this._jsonReader.TryGetDouble(out value);
		public bool TryGetGuid(out Guid value) => this._jsonReader.TryGetGuid(out value);
		public bool TryGetInt16(out short value) => this._jsonReader.TryGetInt16(out value);
		public bool TryGetInt32(out int value) => this._jsonReader.TryGetInt32(out value);
		public bool TryGetInt64(out long value) => this._jsonReader.TryGetInt64(out value);
		public bool TryGetSByte(out sbyte value) => this._jsonReader.TryGetSByte(out value);
		public bool TryGetSingle(out float value) => this._jsonReader.TryGetSingle(out value);
		public bool TryGetUInt16(out ushort value) => this._jsonReader.TryGetUInt16(out value);
		public bool TryGetUInt32(out uint value) => this._jsonReader.TryGetUInt32(out value);
		public bool TryGetUInt64(out ulong value) => this._jsonReader.TryGetUInt64(out value);

		private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>, IDisposable
		{
			internal IMemoryOwner<byte> Buffer { get; }
			internal SequenceSegment? Previous { get; set; }
			private bool _disposed;

			public SequenceSegment(int size, SequenceSegment? previous)
			{
				this.Buffer = MemoryPool<byte>.Shared.Rent(size);
				this.Previous = previous;

				this.Memory = this.Buffer.Memory;
				this.RunningIndex = previous?.RunningIndex + previous?.Memory.Length ?? 0;
			}

			public void SetNext(SequenceSegment next) => this.Next = next;

			public void Dispose()
			{
				if (!this._disposed)
				{
					this._disposed = true;
					this.Buffer.Dispose();
					this.Previous?.Dispose();
				}
			}
		}
	}
}
