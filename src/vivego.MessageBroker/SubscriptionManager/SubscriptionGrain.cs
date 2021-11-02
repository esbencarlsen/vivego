using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Orleans;
using Orleans.Placement;
using Orleans.Serialization;

using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.EventStore;

namespace vivego.MessageBroker.SubscriptionManager;

[Serializable]
public record SubscriptionEventBase;

[Serializable]
public sealed record SubscriptionEvent(string SubscriptionId, SubscriptionType Type, string Pattern) : SubscriptionEventBase;

[Serializable]
public sealed record UnSubscriptionEvent(string SubscriptionId, SubscriptionType Type, string Pattern) : SubscriptionEventBase;

[Serializable]
public sealed record SubscriptionSnapshotEvent(int HashIdx, SubscriptionEvent[] Events) : SubscriptionEventBase;

[ActivationCountBasedPlacement]
public sealed class SubscriptionGrain : Grain, ISubscriptionGrain
{
	private readonly IEventStore _eventStore;
	private readonly SerializationManager _serializationManager;
	private readonly SubscriptionRepository _subscriptionRepository;
	private bool _dirty;
	private IDisposable? _timerRegistration;

	public SubscriptionGrain(
		IEventStore messageBroker,
		IMemoryCache memoryCache,
		SerializationManager serializationManager)
	{
		if (memoryCache is null) throw new ArgumentNullException(nameof(memoryCache));
		_eventStore = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
		_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));

		_subscriptionRepository = new SubscriptionRepository(memoryCache);
	}

	public override async Task OnActivateAsync()
	{
		await base.OnActivateAsync().ConfigureAwait(true);

		bool stopped = false;
		List<SubscriptionEventBase> subscriptionEvents = new();
		await foreach (EventSourceEvent eventSourceEvent in _eventStore
			.GetReverse(MakeBrokerTopic(this.GetPrimaryKeyLong()), -1, CancellationToken.None)
			.TakeWhile(_ => !stopped)
			.ConfigureAwait(true))
		{
			SubscriptionEventBase subscriptionEventBase = _serializationManager
				.DeserializeFromByteArray<SubscriptionEventBase>(eventSourceEvent.Data);

			switch (subscriptionEventBase)
			{
				case SubscriptionEvent subscriptionEvent:
					subscriptionEvents.Add(subscriptionEvent);
					break;
				case UnSubscriptionEvent unSubscriptionEvent:
					subscriptionEvents.Add(unSubscriptionEvent);
					break;
				case SubscriptionSnapshotEvent subscriptionSnapshotEvent:
					stopped = true;
					foreach ((string? subscriptionId, SubscriptionType subscriptionType, string? pattern) in subscriptionSnapshotEvent.Events)
					{
						_subscriptionRepository.Add(subscriptionId, subscriptionType, pattern);
					}

					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		subscriptionEvents.Reverse();
		foreach (SubscriptionEventBase subscriptionEventBase in subscriptionEvents)
		{
			switch (subscriptionEventBase)
			{
				case SubscriptionEvent(var subscriptionId, var subscriptionType, var pattern):
					_subscriptionRepository.Add(subscriptionId, subscriptionType, pattern);
					break;
				case UnSubscriptionEvent(var subscriptionId, var subscriptionType, var pattern):
					_subscriptionRepository.Remove(subscriptionId, subscriptionType, pattern);
					break;
			}
		}

		_timerRegistration = RegisterTimer(_ =>
			{
				if (_dirty)
				{
					_dirty = false;
					SubscriptionSnapshotEvent snapshotEvent = new((int)this.GetPrimaryKeyLong(),
						_subscriptionRepository
							.Get()
							.SelectMany(pair => pair.Value.SubscriptionRegistrationEntries
								.Select(registrationEntry =>
									new SubscriptionEvent(pair.Key, registrationEntry.Type, registrationEntry.Pattern)))
							.ToArray());
					byte[] serializedSnapshotEvent = _serializationManager.SerializeToByteArray(snapshotEvent);
					return _eventStore
						.Append(MakeBrokerTopic(snapshotEvent.HashIdx), serializedSnapshotEvent, TimeSpan.FromDays(1));
				}

				return Task.CompletedTask;
			},
			default,
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(1));
	}

	public override Task OnDeactivateAsync()
	{
		_timerRegistration?.Dispose();
		return base.OnDeactivateAsync();
	}

	public Task<IDictionary<string, HashSet<SubscriptionRegistrationEntry>>> Get()
	{
		IDictionary<string, SubscriptionRegistration> dictionary = _subscriptionRepository.Get();
		IDictionary<string, HashSet<SubscriptionRegistrationEntry>> result = dictionary
			.ToDictionary(pair => pair.Key, pair => pair.Value.SubscriptionRegistrationEntries);
		return Task.FromResult(result);
	}

	public Task Subscribe(string subscriptionId, SubscriptionType type, string pattern)
	{
		if (_subscriptionRepository.Add(subscriptionId, type, pattern))
		{
			_dirty = true;
			SubscriptionEvent subscriptionEvent = new(subscriptionId, type, pattern);
			byte[] serializedSubscriptionEvent = _serializationManager.SerializeToByteArray(subscriptionEvent);
			return _eventStore
				.Append(MakeBrokerTopic(this.GetPrimaryKeyLong()), serializedSubscriptionEvent, TimeSpan.FromDays(1));
		}

		return Task.CompletedTask;
	}

	public Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern)
	{
		if (_subscriptionRepository.Remove(subscriptionId, type, pattern))
		{
			_dirty = true;
			UnSubscriptionEvent unSubscriptionEvent = new(subscriptionId, type, pattern);
			byte[] serializedUnSubscriptionEvent = _serializationManager.SerializeToByteArray(unSubscriptionEvent);
			return _eventStore
				.Append(MakeBrokerTopic(this.GetPrimaryKeyLong()), serializedUnSubscriptionEvent, TimeSpan.FromDays(1));
		}

		return Task.CompletedTask;
	}

	public static string MakeBrokerTopic(long id)
	{
		return $"{nameof(SubscriptionGrain)}_{id}";
	}
}
