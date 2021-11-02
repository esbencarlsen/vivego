using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.EventStore.Append;
using vivego.MessageBroker.EventStore.GetEvent;
using vivego.MessageBroker.EventStore.GetNextEventId;
using vivego.MessageBroker.PublishSubscribe;

namespace vivego.MessageBroker.EventStore;

public sealed class DefaultEventStore : IEventStore
{
	private readonly IMediator _mediator;
	private readonly IPublishSubscribe _publishSubscribe;

	public DefaultEventStore(
		IMediator mediator,
		IPublishSubscribe publishSubscribe)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_publishSubscribe = publishSubscribe ?? throw new ArgumentNullException(nameof(publishSubscribe));
	}

	public Task<long> Append(
		string topic,
		byte[] data,
		TimeSpan? timeToLive = default,
		IDictionary<string, string>? metaData = default,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		return _mediator.Send(new AppendRequest(topic, data, timeToLive, metaData), cancellationToken);
	}

	public Task<EventSourceEvent?> GetEvent(
		string topic,
		long eventId,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		if (eventId < 0) throw new ArgumentOutOfRangeException(nameof(eventId), "Must be >= 0");
		return _mediator.Send(new GetEventSourceEvent(topic, eventId), cancellationToken);
	}

	public Task<long> GetNextEventId(string topic, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		return _mediator.Send(new GetNextEventIdRequest(topic), cancellationToken);
	}

	public async IAsyncEnumerable<EventSourceEvent> StreamingGet(string topic,
		long? fromId,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));

		IAsyncEnumerable<byte[]> publishSubscribeSubscription = _publishSubscribe.Subscribe(topic, cancellationToken);
		IAsyncEnumerable<byte[]> timerSubscription = MakeTimerSubscription(TimeSpan.FromSeconds(15), cancellationToken);
		IAsyncEnumerable<byte[]> subscription = AsyncEnumerableEx.Merge(publishSubscribeSubscription, timerSubscription);

		long? lastEventId = default;

		if (fromId is not null)
		{
			IAsyncEnumerable<EventSourceEvent> replayEvents = this.Get(topic, fromId.Value, cancellationToken);
			await foreach (EventSourceEvent replayEvent in replayEvents
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false))
			{
				lastEventId = replayEvent.EventId;
				yield return replayEvent;
			}
		}

		// Listen for changes
		await foreach (byte[] eventIdBytes in subscription
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			long eventId = eventIdBytes.Length == sizeof(long)
				? BitConverter.ToInt64(eventIdBytes)
				: long.MaxValue;
			if (lastEventId is null || eventId > lastEventId.Value)
			{
				long getFromId = lastEventId + 1 ?? fromId.GetValueOrDefault(eventId);
				await foreach (EventSourceEvent eventSourceEvent in this.Get(topic, getFromId, cancellationToken)
					.ConfigureAwait(false))
				{
					if (lastEventId is null || eventSourceEvent.EventId > lastEventId)
					{
						lastEventId = eventSourceEvent.EventId;
						yield return eventSourceEvent;
					}
				}
			}
		}
	}

	private static async IAsyncEnumerable<byte[]> MakeTimerSubscription(TimeSpan interval,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		using PeriodicTimer periodicTimer = new(interval);
		while (await WaitForNextTickAsync(periodicTimer).ConfigureAwait(false))
		{
			yield return Array.Empty<byte>();
		}

		async Task<bool> WaitForNextTickAsync(PeriodicTimer timer)
		{
			try
			{
				return await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Ignore
			}

			return false;
		}
	}

	public static string GetStoreKey(string topic, long eventId)
	{
		return $"{topic}_{eventId}";
	}
}
