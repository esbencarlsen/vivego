using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.Queue.Model;

namespace vivego.Collection.Queue.Truncate
{
	public sealed class TruncateRequestHandler : IRequestHandler<TruncateRequest>
	{
		private readonly IQueueState _queueState;
		private readonly IKeyValueStore _keyValueStore;

		public TruncateRequestHandler(
			IQueueState queueState,
			IKeyValueStore keyValueStore)
		{
			_queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<Unit> Handle(TruncateRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			QueueState state = await _queueState.GetState(request.Id, cancellationToken).ConfigureAwait(false);

			long head = request.Head.GetValueOrDefault(0);
			long tail = request.Tail.GetValueOrDefault(0);
			if (!request.Fast)
			{
				foreach (long version in state.Range())
				{
					if (version >= head && version < tail)
					{
						continue;
					}

					string key = RequestHandlerHelper.MakeKey(request.Id, version);
					await _keyValueStore
						.DeleteEntry(key, cancellationToken)
						.ConfigureAwait(false);
				}
			}

			state.Head = head;
			state.Tail = tail;
			await _queueState.SetState(request.Id, state, null, cancellationToken).ConfigureAwait(false);

			return Unit.Value;
		}
	}
}
