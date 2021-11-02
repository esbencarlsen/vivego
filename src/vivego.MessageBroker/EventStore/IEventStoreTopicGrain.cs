using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;

namespace vivego.MessageBroker.EventStore;

public interface IEventStoreTopicGrain : IGrainWithStringKey
{
	Task<long> Append(byte[] data,
		IDictionary<string, string>? metaData = default,
		TimeSpan? timeToLive = default);

	[AlwaysInterleave]
	Task<long> GetNextEventId();
}
