using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.EventStore;

public static class EventStoreExtensions
{
	public static async IAsyncEnumerable<EventSourceEvent> Get(
		this IEventStore eventStore,
		string topic,
		long fromId,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(eventStore);
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		if (fromId < 0)
		{
			// Get last id
			long nextId = await eventStore.GetNextEventId(topic, cancellationToken).ConfigureAwait(false);
			if (nextId < 0)
			{
				// Empty
				yield break;
			}

			fromId = nextId + fromId;
			if (fromId < 0)
			{
				fromId = 0;
			}
		}

		long eventId = fromId;
		while (!cancellationToken.IsCancellationRequested)
		{
			EventSourceEvent? eventSourceEvent = await eventStore
				.GetEvent(topic, eventId, cancellationToken)
				.ConfigureAwait(false);
			if (eventSourceEvent is null)
			{
				yield break;
			}

			yield return eventSourceEvent;
			eventId++;
		}
	}

	public static async IAsyncEnumerable<EventSourceEvent> GetReverse(
		this IEventStore eventStore,
		string topic,
		long fromId,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(eventStore);
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		if (fromId < 0)
		{
			// Get last id
			long nextId = await eventStore.GetNextEventId(topic, cancellationToken).ConfigureAwait(false);
			if (nextId < 0)
			{
				// Empty
				yield break;
			}

			fromId = nextId + fromId;
			if (fromId < 0)
			{
				fromId = 0;
			}
		}

		long eventId = fromId;
		while (!cancellationToken.IsCancellationRequested && eventId >= 0)
		{
			EventSourceEvent? eventSourceEvent = await eventStore
				.GetEvent(topic, eventId, cancellationToken)
				.ConfigureAwait(false);
			if (eventSourceEvent is null)
			{
				yield break;
			}

			yield return eventSourceEvent;
			eventId--;
		}
	}
}
