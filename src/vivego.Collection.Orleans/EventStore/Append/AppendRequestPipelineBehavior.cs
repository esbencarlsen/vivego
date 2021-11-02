using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans;
using Orleans.Runtime;

using vivego.Collection.EventStore;
using vivego.Collection.EventStore.Append;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.Orleans.EventStore.Append
{
	public sealed class AppendRequestPipelineBehavior : IPipelineBehavior<AppendRequest, Version>
	{
		private readonly IClusterClient _clusterClient;
		private readonly IEventStore _eventStore;

		public AppendRequestPipelineBehavior(IClusterClient clusterClient, IEventStore eventStore)
		{
			_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		}

		public Task<Version> Handle(AppendRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<Version> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			if (RequestContext.Get(nameof(EventStoreGrain)) is bool inGrain && inGrain)
			{
				return next();
			}

			return _clusterClient
				.GetGrain<IEventStoreGrain>(request.StreamId)
				.Append(_eventStore.Name, request.ExpectedVersion, request.EventDatas);
		}
	}
}
