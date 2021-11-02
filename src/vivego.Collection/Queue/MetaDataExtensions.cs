using System;

using Google.Protobuf;
using Google.Protobuf.Collections;

namespace vivego.Collection.Queue
{
	public static class MetaDataExtensions
	{
		public const string KeyValueTtlKey = "KeyValueTtl";
		public const string StateValueTtlKey = "StateValueTtl";

		public static void SetEventDataKeyValueTtl(this MapField<string, ByteString> metaData, TimeSpan timeSpan)
		{
			if (metaData is null) throw new ArgumentNullException(nameof(metaData));
			byte[] bytes = BitConverter.GetBytes((long)timeSpan.TotalMilliseconds);
			metaData[KeyValueTtlKey] = ByteString.CopyFrom(bytes);
		}

		public static long? GetEventDataKeyValueTtlMilliSeconds(this MapField<string, ByteString> metaData)
		{
			if (metaData is null) throw new ArgumentNullException(nameof(metaData));
			if (metaData.TryGetValue(KeyValueTtlKey, out var byteString))
			{
				long milliseconds = BitConverter.ToInt64(byteString.Span);
				return milliseconds;
			}

			return default;
		}

		public static long? GetEventDataKeyValueTtlSeconds(this MapField<string, ByteString> metaData)
		{
			if (metaData is null) throw new ArgumentNullException(nameof(metaData));
			long? milliseconds = GetEventDataKeyValueTtlMilliSeconds(metaData);
			return milliseconds / 1000;
		}

		public static void SetStateDataKeyValueTtl(this MapField<string, ByteString> metaData, TimeSpan timeSpan)
		{
			if (metaData is null) throw new ArgumentNullException(nameof(metaData));
			byte[] bytes = BitConverter.GetBytes((long)timeSpan.TotalMilliseconds);
			metaData[StateValueTtlKey] = ByteString.CopyFrom(bytes);
		}

		public static long? GetStateDataKeyValueTtlMilliSeconds(this MapField<string, ByteString> metaData)
		{
			if (metaData is null) throw new ArgumentNullException(nameof(metaData));
			if (metaData.TryGetValue(StateValueTtlKey, out var byteString))
			{
				long milliseconds = BitConverter.ToInt64(byteString.Span);
				return milliseconds;
			}

			return default;
		}

		public static long? GetStateDataKeyValueTtlSeconds(this MapField<string, ByteString> metaData)
		{
			if (metaData is null) throw new ArgumentNullException(nameof(metaData));
			long? milliseconds = GetStateDataKeyValueTtlMilliSeconds(metaData);
			return milliseconds / 1000;
		}
	}
}