using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans;
using Orleans.Runtime;

using vivego.Collection.EventStore;
using vivego.Collection.EventStore.GetState;
using vivego.EventStore;

namespace vivego.Collection.Orleans.EventStore.GetState
{
	public sealed class GetStateRequestPipelineBehavior : IPipelineBehavior<GetStateRequest, EventStoreState>
	{
		private readonly IClusterClient _clusterClient;
		private readonly IEventStore _eventStore;

		public GetStateRequestPipelineBehavior(IClusterClient clusterClient, IEventStore eventStore)
		{
			_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		}

		public Task<EventStoreState> Handle(GetStateRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<EventStoreState> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			if (RequestContext.Get(nameof(EventStoreGrain)) is bool inGrain && inGrain)
			{
				return next();
			}

			return _clusterClient
				.GetGrain<IEventStoreStateGrain>(request.StreamId)
				.GetState(_eventStore.Name);
		}
	}
}
