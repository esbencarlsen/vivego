using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Serialization;

using vivego.core;
using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.EventStore;

namespace vivego.MessageBroker.SubscriptionManager;

public sealed class DefaultSubscriptionManager :
	BackgroundService,
	ISubscriptionManager
{
	private readonly IClusterClient _clusterClient;
	private readonly IMemoryCache _memoryCache;
	private readonly IEventStore _eventStore;
	private readonly SerializationManager _serializationManager;
	private readonly IOptions<DefaultSubscriptionManagerOptions> _options;
	private readonly IDictionary<int, SubscriptionRepository> _db = new Dictionary<int, SubscriptionRepository>();
	private readonly AsyncReadersWriterLock _readerWriterLockSlim = new();

	public DefaultSubscriptionManager(
		IClusterClient clusterClient,
		IMemoryCache memoryCache,
		IEventStore eventStore,
		SerializationManager serializationManager,
		IOptions<DefaultSubscriptionManagerOptions> options)
	{
		_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
		_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));
		_options = options ?? throw new ArgumentNullException(nameof(options));

		foreach (int hashIdx in Enumerable.Range(0, options.Value.SubscriptionGrainCount))
		{
			_db[hashIdx] = new SubscriptionRepository(memoryCache);
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _readerWriterLockSlim
			.UseWriterAsync(async () =>
			{
				foreach (int hashIdx in Enumerable
					.Range(0, _options.Value.SubscriptionGrainCount))
				{
					IDictionary<string, HashSet<SubscriptionRegistrationEntry>> db = await _clusterClient
						.GetGrain<ISubscriptionGrain>(hashIdx)
						.Get()
						.ConfigureAwait(false);

					SubscriptionRepository subscriptionRepository = _db[hashIdx];
					foreach ((string? key, HashSet<SubscriptionRegistrationEntry>? value) in db)
					{
						foreach (SubscriptionRegistrationEntry registrationEntry in value)
						{
							subscriptionRepository.Add(key, registrationEntry.Type, registrationEntry.Pattern);
						}
					}
				}
			})
			.ConfigureAwait(false);

		IAsyncEnumerable<EventSourceEvent> subscriptionChanges = Enumerable
			.Range(0, _options.Value.SubscriptionGrainCount)
			.Select(sid => _eventStore
				.StreamingGet(SubscriptionGrain.MakeBrokerTopic(sid), -1, stoppingToken))
			.Merge();

		await foreach (EventSourceEvent eventSourceEvent in subscriptionChanges.ConfigureAwait(false))
		{
			SubscriptionEventBase subscriptionEventBase = _serializationManager
				.DeserializeFromByteArray<SubscriptionEventBase>(eventSourceEvent.Data);

			await _readerWriterLockSlim
				.UseWriterAsync(() =>
				{
					int hashIdx;
					switch (subscriptionEventBase)
					{
						case SubscriptionEvent(var subscriptionId, var subscriptionType, var pattern):
							hashIdx = subscriptionId.GetDeterministicHashCode() % _options.Value.SubscriptionGrainCount;
							_db[hashIdx].Add(subscriptionId, subscriptionType, pattern);
							break;
						case UnSubscriptionEvent(var subscriptionId, var subscriptionType, var pattern):
							hashIdx = subscriptionId.GetDeterministicHashCode() % _options.Value.SubscriptionGrainCount;
							_db[hashIdx].Remove(subscriptionId, subscriptionType, pattern);
							break;
						case SubscriptionSnapshotEvent subscriptionSnapshotEvent:
							SubscriptionRepository subscriptionRepository = new(_memoryCache);
							foreach (SubscriptionEvent subscriptionEvent in subscriptionSnapshotEvent.Events)
							{
								subscriptionRepository.Add(subscriptionEvent.SubscriptionId, subscriptionEvent.Type, subscriptionEvent.Pattern);
							}

							_db[subscriptionSnapshotEvent.HashIdx] = subscriptionRepository;
							break;
					}
				})
				.ConfigureAwait(false);
		}
	}

	public ValueTask<string[]> GetSubscriptionsFromTopic(string topic)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));

		return _readerWriterLockSlim
			.UseReaderAsync(() =>
			{
				return _db
					.SelectMany(pair => pair.Value.GetSubscriptionsFromTopic(topic))
					.ToArray();
			});
	}

	public async Task Subscribe(string subscriptionId, SubscriptionType type, string pattern)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));

		int hashIdx = subscriptionId.GetDeterministicHashCode() % _options.Value.SubscriptionGrainCount;

		await _readerWriterLockSlim
			.UseWriterAsync(() => _db[hashIdx].Add(subscriptionId, type, pattern))
			.ConfigureAwait(false);

		await _clusterClient
			.GetGrain<ISubscriptionGrain>(hashIdx)
			.Subscribe(subscriptionId, type, pattern)
			.ConfigureAwait(false);
	}

	public async Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));

		int hashIdx = subscriptionId.GetDeterministicHashCode() % _options.Value.SubscriptionGrainCount;
		await _readerWriterLockSlim
			.UseWriterAsync(() => _db[hashIdx].Remove(subscriptionId, type, pattern))
			.ConfigureAwait(false);

		await _clusterClient
			.GetGrain<ISubscriptionGrain>(hashIdx)
			.UnSubscribe(subscriptionId, type, pattern)
			.ConfigureAwait(false);
	}
}
