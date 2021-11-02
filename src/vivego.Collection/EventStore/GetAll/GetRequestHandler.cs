using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

using Range = vivego.EventStore.Range;

namespace vivego.Collection.EventStore.GetAll
{
	public sealed class GetRequestHandler :
		IRequestHandler<GetRequest, IAsyncEnumerable<RecordedEvent>>,
		IPipelineBehavior<GetRequest, IAsyncEnumerable<RecordedEvent>>
	{
		private readonly IEventStore _eventStore;
		private readonly IKeyValueStore _keyValueStore;

		public GetRequestHandler(
			IEventStore eventStore,
			IKeyValueStore keyValueStore)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<IAsyncEnumerable<RecordedEvent>> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			EventStoreState state = await _eventStore.GetState(request.StreamId, cancellationToken).ConfigureAwait(false);
			return GetAll(request.StreamId, request.Range, state, cancellationToken);
		}

		public async Task<IAsyncEnumerable<RecordedEvent>> Handle(GetRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<IAsyncEnumerable<RecordedEvent>> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			EventStoreState state = await _eventStore.GetState(request.StreamId, cancellationToken).ConfigureAwait(false);
			long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (state.DeleteBeforeUnixTimeMilliseconds > now)
			{
				return AsyncEnumerable.Empty<RecordedEvent>();
			}

			return GetAll(request.StreamId, request.Range, state, cancellationToken);
		}

		public async IAsyncEnumerable<RecordedEvent> GetAll(string streamId,
			Range range,
			EventStoreState state,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));

			(long start, long end) = RequestHandlerHelper.GetAbsoluteStartEnd(state, range);
			for (long version = start; version <= end; version++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				string key = RequestHandlerHelper.MakeKey(streamId, version);
				KeyValueEntry keyValueEntry = await _keyValueStore
					.Get(key, cancellationToken)
					.ConfigureAwait(false);

				if (keyValueEntry.Value.IsNull())
				{
					continue;
				}

				yield return RecordedEvent.Parser.ParseFrom(keyValueEntry.Value.Data);
			}
		}
	}
}
