using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans;

namespace vivego.MessageBroker.EventStore.GetNextEventId;

public sealed class GetNextEventIdRequestHandler : IRequestHandler<GetNextEventIdRequest, long>
{
	private readonly IClusterClient _clusterClient;

	public GetNextEventIdRequestHandler(IClusterClient clusterClient)
	{
		_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
	}

	public Task<long> Handle(GetNextEventIdRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);

		return _clusterClient
			.GetGrain<IEventStoreTopicGrain>(request.Topic)
			.GetNextEventId();
	}
}
