using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Collection.EventStore.SetState
{
	public sealed class SetStateRequestHandler : IRequestHandler<SetStateRequest>
	{
		private readonly IKeyValueStore _keyValueStore;

		public SetStateRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<Unit> Handle(SetStateRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			string stateKey = RequestHandlerHelper.MakeKey(request.StreamId, -1);
			await _keyValueStore
				.Set(new SetKeyValueEntry
				{
					Key = stateKey,
					ETag = string.Empty,
					Value = request.State.ToNullableBytes(),
					ExpiresInSeconds = request.State.ExpiresInSeconds,
					MetaData =
					{
						{nameof(SetStateRequest), ByteString.Empty}
					}
				}, cancellationToken)
				.ConfigureAwait(false);
			return Unit.Value;
		}
	}
}
