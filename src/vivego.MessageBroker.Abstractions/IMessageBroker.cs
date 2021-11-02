using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.MessageBroker.Abstractions;

[Serializable]
public sealed record MessageBrokerEvent(long EventId, DateTimeOffset CreatedAt, byte[] Data, IDictionary<string, string>? MetaData) : EventSourceEvent(EventId, CreatedAt, Data, MetaData);

public enum SubscriptionType
{
	Glob,
	RegEx
}

[Serializable]
public sealed record MessageBrokerData(byte[] Data, IDictionary<string, string>? MetaData);

public interface IMessageBroker
{
	Task Publish(string topic,
		byte[] data,
		TimeSpan? timeToLive = default,
		IDictionary<string, string>? metaData = default,
		CancellationToken cancellationToken = default);

	Task<MessageBrokerEvent?> GetEvent(
		string subscriptionId,
		long eventId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<MessageBrokerEvent> Get(
		string subscriptionId,
		long fromId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<MessageBrokerEvent> GetReverse(
		string subscriptionId,
		long fromId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<MessageBrokerEvent> StreamingGet(
		string subscriptionId,
		long? fromId,
		CancellationToken cancellationToken = default);

	Task Subscribe(string subscriptionId, SubscriptionType type, string pattern, CancellationToken cancellationToken = default);
	Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern, CancellationToken cancellationToken = default);
}
