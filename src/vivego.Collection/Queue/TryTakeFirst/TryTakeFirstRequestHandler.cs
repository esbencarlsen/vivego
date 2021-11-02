﻿using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.Queue.Model;

namespace vivego.Collection.Queue.TryTakeFirst
{
	public sealed class TryTakeFirstRequestHandler : IRequestHandler<TryTakeFirstRequest, IQueueEntry?>
	{
		private readonly IKeyValueStore _keyValueStore;
		private readonly IQueueState _queueState;
		private readonly IQueue _queue;

		public TryTakeFirstRequestHandler(
			IKeyValueStore keyValueStore,
			IQueueState queueState,
			IQueue queue)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
			_queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
		}

		public async Task<IQueueEntry?> Handle(TryTakeFirstRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			QueueState state = await _queueState.GetState(request.Id, cancellationToken).ConfigureAwait(false);
			if (state is null)
			{
				return default;
			}

			if (state.Head >= state.Tail)
			{
				return default;
			}

			IQueueEntry? queueEntry = await _queue
				.Get(request.Id, state.Head, cancellationToken)
				.ConfigureAwait(false);

			if (queueEntry is not null)
			{
				if (!request.Fast)
				{
					string key = RequestHandlerHelper.MakeKey(request.Id, state.Head);
					await _keyValueStore
						.DeleteEntry(key, cancellationToken)
						.ConfigureAwait(false);
				}

				state.Head++;
				await _queueState.SetState(request.Id, state, null, cancellationToken).ConfigureAwait(false);

				return queueEntry;
			}

			return default;
		}
	}
}
