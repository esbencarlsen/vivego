using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.Queue.Model;

namespace vivego.Collection.Queue.Append
{
	public sealed class AppendRequestHandler : IRequestHandler<AppendRequest, long?>
	{
		private readonly IQueueState _queueState;
		private readonly IKeyValueStore _keyValueStore;

		public AppendRequestHandler(
			IQueueState queueState,
			IKeyValueStore keyValueStore)
		{
			_queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<long?> Handle(AppendRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			QueueState state = await _queueState.GetState(request.Id, cancellationToken).ConfigureAwait(false);

			if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != state.Tail)
			{
				return default;
			}

			long version = state.Tail;
			string key = RequestHandlerHelper.MakeKey(request.Id, version);
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

			state.Tail++;
			await _queueState.SetState(request.Id, state, null, cancellationToken).ConfigureAwait(false);

			return version;
		}
	}
}
