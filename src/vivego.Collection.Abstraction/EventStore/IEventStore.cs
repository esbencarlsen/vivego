using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using vivego.EventStore;
using vivego.ServiceBuilder.Abstractions;

using Range = vivego.EventStore.Range;
using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public sealed class EventStreamOptions
	{
		public long? MaximumEventCount { get; set; }
		public TimeSpan? EventTimeToLive { get; set; }
		public DateTimeOffset? DeleteBefore { get; set; }

		public EventStreamOptions(long? maximumEventCount, TimeSpan? eventTimeToLive, DateTimeOffset? deleteBefore)
		{
			MaximumEventCount = maximumEventCount;
			EventTimeToLive = eventTimeToLive;
			DeleteBefore = deleteBefore;
		}
	}

	public interface IEventStore : INamedService
	{
		Task<Version> Append(string streamId,
			long expectedVersion,
			IEnumerable<EventData> eventDatas,
			CancellationToken cancellationToken = default);
		IAsyncEnumerable<RecordedEvent> Get(string streamId, Range range, CancellationToken cancellationToken = default);
		IAsyncEnumerable<RecordedEvent> GetReverse(string streamId, Range range, CancellationToken cancellationToken = default);
		Task Delete(string streamId, CancellationToken cancellationToken = default);

		Task<EventStreamOptions> GetOptions(string streamId, CancellationToken cancellationToken);
		Task SetOptions(string streamId,
			EventStreamOptions eventStreamOptions,
			CancellationToken cancellationToken);

		Task SetState(string streamId, EventStoreState eventStoreState, CancellationToken cancellationToken);
		[return: NotNull]
		Task<EventStoreState> GetState(string streamId, CancellationToken cancellationToken);
	}

	public interface IEventStore<T>
	{
		Task<Version> Append(string streamId,
			long expectedVersion,
			IEnumerable<T> values,
			CancellationToken cancellationToken = default);
		IAsyncEnumerable<IRecordedEvent<T>> Get(string streamId, Range range, CancellationToken cancellationToken = default);
		IAsyncEnumerable<IRecordedEvent<T>> GetReverse(string streamId, Range range, CancellationToken cancellationToken = default);
		Task Delete(string streamId, CancellationToken cancellationToken = default);

		Task<EventStreamOptions> GetOptions(string streamId, CancellationToken cancellationToken);
		Task SetOptions(string streamId,
			EventStreamOptions eventStreamOptions,
			CancellationToken cancellationToken);
	}
}
