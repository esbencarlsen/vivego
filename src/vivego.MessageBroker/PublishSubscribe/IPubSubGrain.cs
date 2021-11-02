using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;

namespace vivego.MessageBroker.PublishSubscribe;

public interface IPubSubGrain : IGrainWithStringKey
{
	Task Subscribe(INotificationGrainObserver observer);
	ValueTask Publish(byte[] data);
}
