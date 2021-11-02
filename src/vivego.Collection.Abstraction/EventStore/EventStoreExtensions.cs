using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using vivego.EventStore;

using Range = vivego.EventStore.Range;
using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public static class EventStoreExtensions
	{
		public static Task<Version> Append(this IEventStore eventStore,
			string streamId,
			long expectedVersion,
			params EventData[] eventDatas)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Append(streamId, expectedVersion, eventDatas, CancellationToken.None);
		}

		public static Task<Version> Append(this IEventStore eventStore,
			string streamId,
			long expectedVersion,
			CancellationToken cancellationToken,
			params EventData[] eventDatas)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Append(streamId, expectedVersion, eventDatas, cancellationToken);
		}

		public static IAsyncEnumerable<RecordedEvent> GetAll(this IEventStore eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Get(streamId, Ranges.All, cancellationToken);
		}

		public static IAsyncEnumerable<RecordedEvent> GetAllReverse(this IEventStore eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.GetReverse(streamId, Ranges.All, cancellationToken);
		}

		public static IAsyncEnumerable<RecordedEvent> GetFrom(this IEventStore eventStore,
			string streamId,
			long rangeStart,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Get(streamId, new Range
			{
				Start = rangeStart,
				End = -1
			}, cancellationToken);
		}

		public static IAsyncEnumerable<RecordedEvent> GetFromReverse(this IEventStore eventStore,
			string streamId,
			long rangeStart,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.GetReverse(streamId, new Range
			{
				Start = rangeStart,
				End = -1
			}, cancellationToken);
		}

		public static IAsyncEnumerable<RecordedEvent> GetTo(this IEventStore eventStore,
			string streamId,
			long rangeEnd,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Get(streamId, new Range
			{
				Start = 0,
				End = rangeEnd
			}, cancellationToken);
		}

		public static IAsyncEnumerable<RecordedEvent> GetToReverse(this IEventStore eventStore,
			string streamId,
			long rangeEnd,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.GetReverse(streamId, new Range
			{
				Start = 0,
				End = rangeEnd
			}, cancellationToken);
		}

		public static async Task<RecordedEvent?> GetFirst(this IEventStore eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			await foreach (RecordedEvent recordedEvent in eventStore.GetAll(streamId, cancellationToken).ConfigureAwait(false))
			{
				return recordedEvent;
			}

			return default;
		}

		public static async Task<RecordedEvent?> GetLast(this IEventStore eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			await foreach (RecordedEvent recordedEvent in eventStore.GetAllReverse(streamId, cancellationToken).ConfigureAwait(false))
			{
				return recordedEvent;
			}

			return default;
		}
	}
}