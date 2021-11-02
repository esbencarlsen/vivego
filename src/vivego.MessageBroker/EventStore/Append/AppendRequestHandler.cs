using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans;

namespace vivego.MessageBroker.EventStore.Append;

public sealed class AppendRequestHandler : IRequestHandler<AppendRequest, long>
{
	private readonly IClusterClient _clusterClient;

	public AppendRequestHandler(IClusterClient clusterClient)
	{
		_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
	}

	public Task<long> Handle(AppendRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		return _clusterClient
			.GetGrain<IEventStoreTopicGrain>(request.Topic)
			.Append(request.Data, request.MetaData, request.TimeToLive);
	}
}
