using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Orleans.Serialization;

using vivego.core;
using vivego.KeyValue;
using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.EventStore;
using vivego.MessageBroker.SubscriptionManager;

namespace vivego.MessageBroker.MessageBroker;

public sealed class DefaultMessageBroker : IMessageBroker
{
	private readonly IEventStore _eventStore;
	private readonly IKeyValueStore _keyValueStore;
	private readonly ISubscriptionManager _subscriptionManager;
	private readonly SerializationManager _serializationManager;

	public DefaultMessageBroker(
		IEventStore eventStore,
		IKeyValueStore keyValueStore,
		ISubscriptionManager subscriptionManager,
		SerializationManager serializationManager)
	{
		_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		_subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
		_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));
	}

	public async Task Publish(string topic,
		byte[] data,
		TimeSpan? timeToLive = default,
		IDictionary<string, string>? metaData = default,
		CancellationToken cancellationToken = default)
	{
		string[] subscriptionIds = await _subscriptionManager
			.GetSubscriptionsFromTopic(topic)
			.ConfigureAwait(false);
		if (subscriptionIds.Any())
		{
			Guid key = Guid.NewGuid();
			string keyString = key.ToString("D");
			MessageBrokerData messageBrokerData = new(data, metaData);
			byte[] serializedMessageBrokerData = _serializationManager.SerializeToByteArray(messageBrokerData);

			await _keyValueStore
				.Set(keyString, serializedMessageBrokerData, default, timeToLive, cancellationToken)
				.ConfigureAwait(false);

			byte[] keyBytes = key.ToByteArray();
			await Parallel
				.ForEachAsync(subscriptionIds, cancellationToken,
					async (subscriptionId, ct) => await _eventStore
						.Append(subscriptionId, keyBytes, timeToLive, default, ct)
						.ConfigureAwait(false))
				.ConfigureAwait(false);
		}
	}

	public async Task<MessageBrokerEvent?> GetEvent(string subscriptionId, long eventId, CancellationToken cancellationToken = default)
	{
		EventSourceEvent? eventSourceEvent = await _eventStore
			.GetEvent(subscriptionId, eventId, cancellationToken)
			.ConfigureAwait(false);
		if (eventSourceEvent is null)
		{
			return default;
		}

		return await GetSourceEvent(eventSourceEvent, cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<MessageBrokerEvent> Get(string subscriptionId, long fromId, CancellationToken cancellationToken = default)
	{
		return _eventStore
			.Get(subscriptionId, fromId, cancellationToken)
			.SelectAwait(@event => GetSourceEvent(@event, cancellationToken))
			.WhereNotNull();
	}

	public IAsyncEnumerable<MessageBrokerEvent> GetReverse(string subscriptionId, long fromId, CancellationToken cancellationToken = default)
	{
		return _eventStore
			.GetReverse(subscriptionId, fromId, cancellationToken)
			.SelectAwait(@event => GetSourceEvent(@event, cancellationToken))
			.WhereNotNull();
	}

	public IAsyncEnumerable<MessageBrokerEvent> StreamingGet(string subscriptionId, long? fromId, CancellationToken cancellationToken = default)
	{
		return _eventStore
			.StreamingGet(subscriptionId, fromId, cancellationToken)
			.SelectAwait(@event => GetSourceEvent(@event, cancellationToken))
			.WhereNotNull();
	}

	public Task Subscribe(string subscriptionId, SubscriptionType type, string pattern, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));
		return _subscriptionManager.Subscribe(subscriptionId, type, pattern);
	}

	public Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));
		return _subscriptionManager.UnSubscribe(subscriptionId, type, pattern);
	}

	private async ValueTask<MessageBrokerEvent?> GetSourceEvent(EventSourceEvent eventSourceEventReference, CancellationToken cancellationToken)
	{
		Guid key = new(eventSourceEventReference.Data);
		string keyString = key.ToString("D");
		byte[]? serializedMessageBrokerData = await _keyValueStore
			.GetValue(keyString, cancellationToken)
			.ConfigureAwait(false);
		if (serializedMessageBrokerData is null)
		{
			return default;
		}

		(byte[]? bytes, IDictionary<string, string>? metaData) = _serializationManager
			.DeserializeFromByteArray<MessageBrokerData>(serializedMessageBrokerData);

		return new MessageBrokerEvent(
			eventSourceEventReference.EventId,
			eventSourceEventReference.CreatedAt,
			bytes,
			metaData);
	}
}
