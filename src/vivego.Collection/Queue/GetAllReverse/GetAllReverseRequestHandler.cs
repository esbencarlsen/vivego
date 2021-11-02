using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Queue.Model;

namespace vivego.Collection.Queue.GetAllReverse
{
	public sealed class GetAllReverseRequestHandler : IRequestHandler<GetAllReverseRequest, IAsyncEnumerable<IQueueEntry>>
	{
		private readonly IQueue _queue;
		private readonly IQueueState _queueState;

		public GetAllReverseRequestHandler(
			IQueue queue,
			IQueueState queueState)
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
		}

		public Task<IAsyncEnumerable<IQueueEntry>> Handle(GetAllReverseRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			return Task.FromResult(GetAll(request, cancellationToken));
		}

		private async IAsyncEnumerable<IQueueEntry> GetAll(GetAllReverseRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			QueueState state = await _queueState.GetState(request.Id, cancellationToken).ConfigureAwait(false);
			foreach (long version in state.ReverseRange(request.Skip))
			{
				IQueueEntry? queueEntry = await _queue.Get(request.Id, version, cancellationToken).ConfigureAwait(false);
				if (queueEntry is not null)
				{
					yield return queueEntry;
				}
			}
		}
	}
}
