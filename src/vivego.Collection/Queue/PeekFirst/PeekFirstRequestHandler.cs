using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Queue.Model;

namespace vivego.Collection.Queue.PeekFirst
{
	public sealed class PeekFirstRequestHandler : IRequestHandler<PeekFirstRequest, IQueueEntry?>
	{
		private readonly IQueueState _queueState;
		private readonly IQueue _queue;

		public PeekFirstRequestHandler(
			IQueueState queueState,
			IQueue queue)
		{
			_queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
		}

		public async Task<IQueueEntry?> Handle(PeekFirstRequest request, CancellationToken cancellationToken)
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

			return await _queue
				.Get(request.Id, state.Head, cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
