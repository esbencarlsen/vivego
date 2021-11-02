using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.MessageBroker.PublishSubscribe;

public interface IPublishSubscribe
{
	ValueTask Publish(string topic, byte[] data);
	IAsyncEnumerable<byte[]> Subscribe(string topic, CancellationToken cancellationToken = default);
}
