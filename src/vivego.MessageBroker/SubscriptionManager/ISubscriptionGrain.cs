using System.Collections.Generic;
using System.Threading.Tasks;

using Orleans;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.SubscriptionManager;

public interface ISubscriptionGrain : IGrainWithIntegerKey
{
	Task<IDictionary<string, HashSet<SubscriptionRegistrationEntry>>> Get();
	Task Subscribe(string subscriptionId, SubscriptionType type, string pattern);
	Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern);
}
