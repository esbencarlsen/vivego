using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Collection.EventStore.GetState
{
	public sealed class GetStateRequestHandler : IRequestHandler<GetStateRequest, EventStoreState>
	{
		private readonly IKeyValueStore _keyValueStore;

		public GetStateRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public Task<EventStoreState> Handle(GetStateRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			return ReadState(request.StreamId, cancellationToken);
		}

		[return: NotNull]
		private async Task<EventStoreState> ReadState(string streamId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));

			string stateKey = RequestHandlerHelper.MakeKey(streamId, -1);
			KeyValueEntry eventStoreState = await _keyValueStore
				.Get(stateKey, cancellationToken)
				.ConfigureAwait(false);
			if (eventStoreState.Value.IsNull())
			{
				return new EventStoreState
				{
					Count = 0,
					Version = -1,
					CreatedAtUnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
					MaximumEventCount = -1,
					ExpiresInSeconds = -1,
					DeleteBeforeUnixTimeMilliseconds = -1
				};
			}

			return EventStoreState.Parser.ParseFrom(eventStoreState.Value.Data);
		}
	}
}
