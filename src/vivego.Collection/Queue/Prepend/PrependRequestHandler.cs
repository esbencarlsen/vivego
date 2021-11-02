using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.Queue.Model;

namespace vivego.Collection.Queue.Prepend
{
	public sealed class PrependRequestHandler : IRequestHandler<PrependRequest, long?>
	{
		private readonly IQueueState _queueState;
		private readonly IKeyValueStore _keyValueStore;

		public PrependRequestHandler(
			IQueueState queueState,
			IKeyValueStore keyValueStore)
		{
			_queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<long?> Handle(PrependRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			QueueState state = await _queueState.GetState(request.Id, cancellationToken).ConfigureAwait(false);
			if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != state.Head - 1)
			{
				return default;
			}

			state.Head--;
			string key = RequestHandlerHelper.MakeKey(request.Id, state.Head);
			SetKeyValueEntry setKeyValueEntry = new()
			{
				Key = key,
				Value = request.Data.ToNullableBytes(),
				ETag = string.Empty,
				ExpiresInSeconds = request.ExpiresIn.HasValue ? (long) request.ExpiresIn.Value.TotalSeconds : 0
			};

			await _keyValueStore
				.Set(setKeyValueEntry, cancellationToken)
				.ConfigureAwait(false);
			await _queueState.SetState(request.Id, state, null, cancellationToken).ConfigureAwait(false);
			return state.Head;
		}
	}
}
