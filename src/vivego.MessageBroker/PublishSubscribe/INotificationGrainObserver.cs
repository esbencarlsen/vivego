using Orleans;

namespace vivego.MessageBroker.PublishSubscribe;

public interface INotificationGrainObserver : IGrainObserver
{
	void Notify(string topic, byte[] data);
}
