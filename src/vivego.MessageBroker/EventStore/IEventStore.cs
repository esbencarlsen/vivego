using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.EventStore;

public interface IEventStore
{
	Task<long> Append(
		string topic,
		byte[] data,
		TimeSpan? timeToLive = default,
		IDictionary<string, string>? metaData = default,
		CancellationToken cancellationToken = default);

	Task<EventSourceEvent?> GetEvent(
		string topic,
		long eventId,
		CancellationToken cancellationToken);

	Task<long> GetNextEventId(string topic, CancellationToken cancellationToken = default);

	IAsyncEnumerable<EventSourceEvent> StreamingGet(
		string topic,
		long? fromId,
		CancellationToken cancellationToken = default);
}
