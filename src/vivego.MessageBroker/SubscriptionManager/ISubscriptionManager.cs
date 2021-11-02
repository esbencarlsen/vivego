using System.Threading.Tasks;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.SubscriptionManager;

public interface ISubscriptionManager
{
	ValueTask<string[]> GetSubscriptionsFromTopic(string topic);
	Task Subscribe(string subscriptionId, SubscriptionType type, string pattern);
	Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern);
}
