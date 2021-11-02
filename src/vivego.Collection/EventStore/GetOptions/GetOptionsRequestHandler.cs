using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;

namespace vivego.Collection.EventStore.GetOptions
{
	public sealed class GetOptionsRequestHandler : IRequestHandler<GetOptionsRequest, EventStreamOptions>
	{
		private readonly IEventStore _eventStore;

		public GetOptionsRequestHandler(IEventStore eventStore)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		}

		public async Task<EventStreamOptions> Handle(GetOptionsRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			EventStoreState state = await _eventStore.GetState(request.StreamId, cancellationToken).ConfigureAwait(false);
			return new EventStreamOptions(state.MaximumEventCount,
				state.ExpiresInSeconds <= 0 ? default : TimeSpan.FromSeconds(state.ExpiresInSeconds),
				state.DeleteBeforeUnixTimeMilliseconds <= 0 ? default : DateTimeOffset.FromUnixTimeMilliseconds(state.DeleteBeforeUnixTimeMilliseconds));
		}
	}
}
