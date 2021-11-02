using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Collection.EventStore.Delete
{
	public sealed class DeleteRequestHandler : IRequestHandler<DeleteRequest>
	{
		private readonly IEventStore _eventStore;
		private readonly IKeyValueStore _keyValueStore;

		public DeleteRequestHandler(
			IEventStore eventStore,
			IKeyValueStore keyValueStore)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<Unit> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			EventStoreState state = await _eventStore.GetState(request.StreamId, cancellationToken).ConfigureAwait(false);
			if (state.Version < 0)
			{
				return Unit.Value;
			}

			while (state.Count-- > 0 && state.Version >= 0)
			{
				string key = RequestHandlerHelper.MakeKey(request.StreamId, state.Version--);
				await _keyValueStore
					.Delete(new DeleteKeyValueEntry
					{
						Key = key,
						ETag = string.Empty
					}, cancellationToken)
					.ConfigureAwait(false);
			}

			string stateKey = RequestHandlerHelper.MakeKey(request.StreamId, -1);
			await _keyValueStore
				.Delete(new DeleteKeyValueEntry
				{
					Key = stateKey,
					ETag = string.Empty
				}, cancellationToken)
				.ConfigureAwait(false);
			return Unit.Value;
		}
	}
}
