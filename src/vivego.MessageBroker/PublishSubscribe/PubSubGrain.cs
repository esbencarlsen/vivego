using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Orleans;
using Orleans.Placement;

namespace vivego.MessageBroker.PublishSubscribe;

[PreferLocalPlacement]
public sealed class PubSubGrain : PeriodicWriteStateGrain<PubSubGrainState>, IPubSubGrain
{
	private readonly GrainObserverManager<INotificationGrainObserver> _subscribers = new();

	public override async Task OnActivateAsync()
	{
		await base.OnActivateAsync().ConfigureAwait(true);
		foreach (INotificationGrainObserver grainObserver in State.Observers)
		{
			_subscribers.Subscribe(grainObserver);
		}
	}

	public Task Subscribe(INotificationGrainObserver observer)
	{
		_subscribers.Subscribe(observer);
		State.Observers.Add(observer);
		return WriteStateAsync();
	}

	public async ValueTask Publish(byte[] data)
	{
		IEnumerable<INotificationGrainObserver> defuncGrainObservers = _subscribers
			.Notify(grainObserver => grainObserver.Notify(this.GetPrimaryKeyString(), data));

		bool removedObserver = defuncGrainObservers
			.Aggregate(false, (current, grainObserver) => current | State.Observers.Remove(grainObserver));

		if (removedObserver)
		{
			await WriteStateAsync().ConfigureAwait(true);
		}
	}
}
