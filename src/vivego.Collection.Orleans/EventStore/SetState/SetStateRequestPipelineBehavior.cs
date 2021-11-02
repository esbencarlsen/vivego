using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans;
using Orleans.Runtime;

using vivego.Collection.EventStore;
using vivego.Collection.EventStore.SetState;

namespace vivego.Collection.Orleans.EventStore.SetState
{
	public sealed class SetStateRequestPipelineBehavior : IPipelineBehavior<SetStateRequest, Unit>
	{
		private readonly IClusterClient _clusterClient;
		private readonly IEventStore _eventStore;

		public SetStateRequestPipelineBehavior(IClusterClient clusterClient, IEventStore eventStore)
		{
			_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		}

		public async Task<Unit> Handle(SetStateRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<Unit> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			if (RequestContext.Get(nameof(EventStoreGrain)) is bool inGrain && inGrain)
			{
				return await next().ConfigureAwait(false);
			}

			await _clusterClient
				.GetGrain<IEventStoreGrain>(request.StreamId)
				.SetState(_eventStore.Name, request.State)
				.ConfigureAwait(false);
			return Unit.Value;
		}
	}
}
