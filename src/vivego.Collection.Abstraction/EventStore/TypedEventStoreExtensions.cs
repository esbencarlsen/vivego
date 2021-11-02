using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Range = vivego.EventStore.Range;
using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public static class TypedEventStoreExtensions
	{
		public static Task<Version> Append<T>(this IEventStore<T> eventStore,
			string streamId,
			long expectedVersion,
			params T[] eventDatas)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Append(streamId, expectedVersion, eventDatas, CancellationToken.None);
		}

		public static Task<Version> Append<T>(this IEventStore<T> eventStore,
			string streamId,
			long expectedVersion,
			CancellationToken cancellationToken,
			params T[] eventDatas)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Append(streamId, expectedVersion, eventDatas, cancellationToken);
		}

		public static IAsyncEnumerable<IRecordedEvent<T>> GetAll<T>(this IEventStore<T> eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.Get(streamId, Ranges.All, cancellationToken);
		}

		public static IAsyncEnumerable<IRecordedEvent<T>> GetAllReverse<T>(this IEventStore<T> eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			return eventStore.GetReverse(streamId, Ranges.All, cancellationToken);
		}

		public static IAsyncEnumerable<IRecordedEvent<T>> GetFrom<T>(this IEventStore<T> eventStore,
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

		public static IAsyncEnumerable<IRecordedEvent<T>> GetFromReverse<T>(this IEventStore<T> eventStore,
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

		public static IAsyncEnumerable<IRecordedEvent<T>> GetTo<T>(this IEventStore<T> eventStore,
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

		public static IAsyncEnumerable<IRecordedEvent<T>> GetToReverse<T>(this IEventStore<T> eventStore,
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

		public static async Task<IRecordedEvent<T>?> GetFirst<T>(this IEventStore<T> eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			await foreach (IRecordedEvent<T> recordedEvent in eventStore.GetAll(streamId, cancellationToken).ConfigureAwait(false))
			{
				return recordedEvent;
			}

			return default;
		}

		public static async Task<IRecordedEvent<T>?> GetLast<T>(this IEventStore<T> eventStore,
			string streamId,
			CancellationToken cancellationToken = default)
		{
			if (eventStore is null) throw new ArgumentNullException(nameof(eventStore));
			await foreach (IRecordedEvent<T> recordedEvent in eventStore.GetAllReverse(streamId, cancellationToken).ConfigureAwait(false))
			{
				return recordedEvent;
			}

			return default;
		}
	}
}