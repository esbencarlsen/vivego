using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Proto;
using Proto.Cluster;

namespace vivego.ProtoActor;

public sealed class ActorSystemHostedService : IHostedService
{
	private readonly ActorSystem _actorSystem;

	public ActorSystemHostedService(ActorSystem actorSystem)
	{
		_actorSystem = actorSystem;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		return _actorSystem.Cluster().StartMemberAsync();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return _actorSystem.Cluster().ShutdownAsync();
	}
}
