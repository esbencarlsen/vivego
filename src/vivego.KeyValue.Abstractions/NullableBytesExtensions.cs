using System;

using Google.Protobuf;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue;

public static class NullableBytesExtensions
{
	public static readonly NullableBytes EmptyNullableBytes = new()
	{
		Null = NullValue.NullValue
	};

	public static NullableBytes ToNullableBytes(this byte[]? bytes)
	{
		if (bytes is null)
		{
			return EmptyNullableBytes;
		}

		return new NullableBytes
		{
			Data = ByteString.CopyFrom(bytes)
		};
	}

	public static NullableBytes ToNullableBytes(this IMessage message)
	{
		return new()
		{
			Data = message.ToByteString()
		};
	}

	public static byte[]? ToBytes(this NullableBytes nullableBytes)
	{
		if (nullableBytes is null) throw new ArgumentNullException(nameof(nullableBytes));
		switch (nullableBytes.KindCase)
		{
			case NullableBytes.KindOneofCase.None:
			case NullableBytes.KindOneofCase.Null:
				return default;
			case NullableBytes.KindOneofCase.Data:
				return nullableBytes.Data.ToByteArray();
			default:
				throw new ArgumentOutOfRangeException(nameof(nullableBytes));
		}
	}

	public static bool IsNull(this NullableBytes nullableBytes)
	{
		if (nullableBytes is null) throw new ArgumentNullException(nameof(nullableBytes));
		switch (nullableBytes.KindCase)
		{
			case NullableBytes.KindOneofCase.None:
			case NullableBytes.KindOneofCase.Null:
				return true;
			case NullableBytes.KindOneofCase.Data:
				return false;
			default:
				throw new ArgumentOutOfRangeException(nameof(nullableBytes));
		}
	}
}
