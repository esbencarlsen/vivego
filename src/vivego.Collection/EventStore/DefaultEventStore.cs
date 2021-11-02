using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.EventStore.Append;
using vivego.Collection.EventStore.GetAll;
using vivego.Collection.EventStore.GetOptions;
using vivego.Collection.EventStore.GetReverse;
using vivego.Collection.EventStore.GetState;
using vivego.Collection.EventStore.SetOptions;
using vivego.Collection.EventStore.SetState;
using vivego.core;
using vivego.EventStore;

using Range = vivego.EventStore.Range;
using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public sealed class DefaultEventStore : IEventStore
	{
		private readonly IMediator _mediator;

		public DefaultEventStore(
			string name,
			IMediator mediator)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

		public string Name { get; }

		public Task<Version> Append(string streamId,
			long expectedVersion,
			IEnumerable<EventData> eventDatas,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			if (eventDatas is null) throw new ArgumentNullException(nameof(eventDatas));

			return _mediator.Send(new AppendRequest(streamId, expectedVersion, eventDatas), cancellationToken);
		}

		public IAsyncEnumerable<RecordedEvent> Get(string streamId,
			Range range,
			CancellationToken cancellationToken = default)
		{
			if (range is null) throw new ArgumentNullException(nameof(range));
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator
				.Send(new GetRequest(streamId, range), cancellationToken)
				.Unwrap(cancellationToken);
		}

		public IAsyncEnumerable<RecordedEvent> GetReverse(string streamId,
			Range range,
			CancellationToken cancellationToken = default)
		{
			if (range is null) throw new ArgumentNullException(nameof(range));
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator
				.Send(new GetReverseRequest(streamId, range), cancellationToken)
				.Unwrap(cancellationToken);
		}

		public Task Delete(string streamId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator.Send(new Delete.DeleteRequest(streamId), cancellationToken);
		}

		public Task<EventStreamOptions> GetOptions(string streamId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator.Send(new GetOptionsRequest(streamId), cancellationToken);
		}

		public Task SetOptions(string streamId,
			EventStreamOptions eventStreamOptions,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator.Send(new SetOptionsRequest(streamId, eventStreamOptions, 0), cancellationToken);
		}

		public Task SetState(string streamId,
			EventStoreState eventStoreState,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator.Send(new SetStateRequest(streamId, eventStoreState), cancellationToken);
		}

		public Task<EventStoreState> GetState(string streamId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			return _mediator.Send(new GetStateRequest(streamId), cancellationToken);
		}
	}
}
