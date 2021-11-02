using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.Queue.Model;

namespace vivego.Collection.Queue.GetState
{
	public sealed class GetStateRequestHandler : IRequestHandler<GetStateRequest, QueueState>
	{
		private readonly IKeyValueStore _keyValueStore;

		public GetStateRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<QueueState> Handle(GetStateRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			string stateKey = _keyValueStore.Name + "_state";
			KeyValueEntry eventStoreState = await _keyValueStore
				.Get(stateKey, cancellationToken)
				.ConfigureAwait(false);
			if (eventStoreState.Value.IsNull())
			{
				return new QueueState
				{
					CreatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
				};
			}

			return QueueState.Parser.ParseFrom(eventStoreState.Value.Data);
		}
	}
}
