using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

using Range = vivego.EventStore.Range;

namespace vivego.Collection.EventStore.GetReverse
{
	public sealed class GetReverseRequestHandler : IRequestHandler<GetReverseRequest, IAsyncEnumerable<RecordedEvent>>
	{
		private readonly IEventStore _eventStore;
		private readonly IKeyValueStore _keyValueStore;

		public GetReverseRequestHandler(
			IEventStore eventStore,
			IKeyValueStore keyValueStore)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public Task<IAsyncEnumerable<RecordedEvent>> Handle(GetReverseRequest request,
			CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			return Task.FromResult(GetReverse(request.StreamId, request.Range, cancellationToken));
		}

		public async IAsyncEnumerable<RecordedEvent> GetReverse(string streamId,
			Range range,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (range is null) throw new ArgumentNullException(nameof(range));
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));

			EventStoreState state = await _eventStore.GetState(streamId, cancellationToken).ConfigureAwait(false);
			long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (state.DeleteBeforeUnixTimeMilliseconds > now)
			{
				yield break;
			}

			(long start, long end) = RequestHandlerHelper.GetAbsoluteStartEnd(state, range);
			for (long version = end; version >= start; version--)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				string key = RequestHandlerHelper.MakeKey(streamId, version);
				KeyValueEntry keyValueEntry = await _keyValueStore
					.Get(key, cancellationToken)
					.ConfigureAwait(false);

				yield return RecordedEvent.Parser.ParseFrom(keyValueEntry.Value.Data);
			}
		}
	}
}
