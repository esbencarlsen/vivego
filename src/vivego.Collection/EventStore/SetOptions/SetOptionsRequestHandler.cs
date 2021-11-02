using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;

namespace vivego.Collection.EventStore.SetOptions
{
	public sealed class SetOptionsRequestHandler : IRequestHandler<SetOptionsRequest>
	{
		private readonly IEventStore _eventStore;

		public SetOptionsRequestHandler(IEventStore eventStore)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		}

		public async Task<Unit> Handle(SetOptionsRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			EventStoreState state = await _eventStore.GetState(request.StreamId, cancellationToken).ConfigureAwait(false);
			state.ExpiresInSeconds = (long) (request.EventStreamOptions.EventTimeToLive?.TotalSeconds ?? 0);
			state.MaximumEventCount = request.EventStreamOptions.MaximumEventCount ?? 0;
			state.DeleteBeforeUnixTimeMilliseconds = request.EventStreamOptions.DeleteBefore?.ToUnixTimeMilliseconds() ?? 0;
			await _eventStore.SetState(request.StreamId, state, cancellationToken).ConfigureAwait(false);
			return Unit.Value;
		}
	}
}
