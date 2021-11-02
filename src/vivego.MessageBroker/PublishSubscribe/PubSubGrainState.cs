using System.Collections.Generic;

namespace vivego.MessageBroker.PublishSubscribe;

public sealed class PubSubGrainState
{
	public HashSet<INotificationGrainObserver> Observers { get; } = new();
}
