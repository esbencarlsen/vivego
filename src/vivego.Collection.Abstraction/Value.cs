using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;

#pragma warning disable CA2225
namespace vivego.Collection
{
	public sealed record Value : IEquatable<Value>
	{
		public static Value Empty { get; } = new(Array.Empty<byte>());

		public byte[] Data { get; }

		public bool IsEmpty => Data.Length == 0;
		public string AsString => Encoding.UTF8.GetString(Data);
		public int AsInt32 => BitConverter.ToInt32(Data, 0);
		public Guid AsGuid => new(Data);
		public long AsInt64 => BitConverter.ToInt64(Data, 0);
		public DateTime AsDateTime => new(BitConverter.ToInt64(Data, 0), DateTimeKind.Utc);
		public DateTimeOffset AsDateTimeOffset => new(BitConverter.ToInt64(Data, 0), TimeSpan.Zero);
		public ByteString AsByteString => ByteString.CopyFrom(Data);

		public Value(byte[] data) => Data = data;
		public Value(string data) => Data = Encoding.UTF8.GetBytes(data);
		public Value(Guid data) => Data = data.ToByteArray();
		public Value(int data) => Data = BitConverter.GetBytes(data);
		public Value(long data) => Data = BitConverter.GetBytes(data);
		public Value(DateTime data) => Data = BitConverter.GetBytes(data.Ticks);
		public Value(DateTimeOffset data) => Data = BitConverter.GetBytes(data.UtcTicks);
		public Value(ByteString byteString) => Data = byteString?.ToByteArray() ?? Array.Empty<byte>();

		public static implicit operator Value(byte[] data) => new(data);
		public static implicit operator Value(string data) => new(data);
		public static implicit operator Value(Guid data) => new(data);
		public static implicit operator Value(int data) => new(data);
		public static implicit operator Value(long data) => new(data);
		public static implicit operator Value(DateTime data) => new(data);
		public static implicit operator Value(DateTimeOffset data) => new(data);
		public static implicit operator Value(ByteString data) => new(data);

		public static explicit operator byte[](Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.Data;
		}

		public static explicit operator string(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.AsString;
		}

		public static explicit operator Guid(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.AsGuid;
		}

		public static explicit operator int(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.AsInt32;
		}

		public static explicit operator long(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.AsInt64;
		}

		public static explicit operator DateTime(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.AsDateTime;
		}

		public static explicit operator DateTimeOffset(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return value.AsDateTimeOffset;
		}

		public static explicit operator ByteString(Value value)
		{
			if (value is null) throw new ArgumentNullException(nameof(value));
			return ByteString.CopyFrom(value.Data);
		}

		public bool Equals(Value? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Comparer.Equals(this, other);
		}

		public override int GetHashCode()
		{
			return Comparer.GetHashCode(this);
		}

		private sealed class DataEqualityComparer : IEqualityComparer<Value>
		{
			public bool Equals(Value? x, Value? y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (x is null) return false;
				if (y is null) return false;
				if (x.GetType() != y.GetType()) return false;
				return x.Data.AsSpan().SequenceEqual(y.Data.AsSpan());
			}

			public int GetHashCode(Value obj)
			{
				if (obj is null) throw new ArgumentNullException(nameof(obj));
				byte[] data = obj.Data;
				unchecked
				{
					int hash1 = (5381 << 16) + 5381;
					int hash2 = hash1;

					for (int i = 0; i < data.Length; i += 2)
					{
						hash1 = ((hash1 << 5) + hash1) ^ data[i];
						if (i == data.Length - 1)
						{
							break;
						}

						hash2 = ((hash2 << 5) + hash2) ^ data[i + 1];
					}

					return Math.Abs(hash1 + hash2 * 1566083941);
				}
			}
		}

		public static IEqualityComparer<Value> Comparer { get; } = new DataEqualityComparer();
	}
}
