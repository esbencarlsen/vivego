using System;

using Google.Protobuf;

using vivego.EventStore;

namespace vivego.Collection.EventStore
{
	public static class EventDataExtensions
	{
		public const string KeyValueTtlKey = "KeyValueTtl";
		public const string StateValueTtlKey = "StateValueTtl";

		public static EventData SetEventDataKeyValueTtl(this EventData eventData, TimeSpan timeSpan)
		{
			if (eventData is null) throw new ArgumentNullException(nameof(eventData));
			byte[] bytes = BitConverter.GetBytes((long)timeSpan.TotalMilliseconds);
			eventData.Data[KeyValueTtlKey] = ByteString.CopyFrom(bytes);
			return eventData;
		}

		public static long? GetEventDataKeyValueTtlMilliSeconds(this EventData eventData)
		{
			if (eventData is null) throw new ArgumentNullException(nameof(eventData));
			if (eventData.Data.TryGetValue(KeyValueTtlKey, out var byteString))
			{
				long milliseconds = BitConverter.ToInt64(byteString.Span);
				return milliseconds;
			}

			return default;
		}

		public static long? GetEventDataKeyValueTtlSeconds(this EventData eventData)
		{
			if (eventData is null) throw new ArgumentNullException(nameof(eventData));
			long? milliseconds = GetEventDataKeyValueTtlMilliSeconds(eventData);
			return milliseconds / 1000;
		}

		public static EventData SetStateDataKeyValueTtl(this EventData eventData, TimeSpan timeSpan)
		{
			if (eventData is null) throw new ArgumentNullException(nameof(eventData));
			byte[] bytes = BitConverter.GetBytes((long)timeSpan.TotalMilliseconds);
			eventData.Data[StateValueTtlKey] = ByteString.CopyFrom(bytes);
			return eventData;
		}

		public static long? GetStateDataKeyValueTtlMilliSeconds(this EventData eventData)
		{
			if (eventData is null) throw new ArgumentNullException(nameof(eventData));
			if (eventData.Data.TryGetValue(StateValueTtlKey, out var byteString))
			{
				long milliseconds = BitConverter.ToInt64(byteString.Span);
				return milliseconds;
			}

			return default;
		}

		public static long? GetStateDataKeyValueTtlSeconds(this EventData eventData)
		{
			if (eventData is null) throw new ArgumentNullException(nameof(eventData));
			long? milliseconds = GetStateDataKeyValueTtlMilliSeconds(eventData);
			return milliseconds / 1000;
		}
	}
}