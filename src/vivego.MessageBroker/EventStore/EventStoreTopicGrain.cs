using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Placement;
using Orleans.Serialization;

using vivego.KeyValue;
using vivego.MessageBroker.Abstractions;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.MessageBroker.EventStore;

[PreferLocalPlacement]
public sealed class EventStoreTopicGrain : Grain, IEventStoreTopicGrain
{
	private readonly IOptions<EventStoreOptions> _options;
	private readonly SerializationManager _serializationManager;
	private long _eventId = -1;
	private bool _dirty;
	private IDisposable? _timerRegistration;
	private readonly IKeyValueStore _keyValueStore;

	public EventStoreTopicGrain(
		SerializationManager serializationManager,
		IServiceManager<IKeyValueStore> serviceManager,
		IOptions<EventStoreOptions> options)
	{
		if (serviceManager is null) throw new ArgumentNullException(nameof(serviceManager));
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));

		_keyValueStore = string.IsNullOrEmpty(options.Value.KeyValueStoreName)
			? serviceManager.GetAll().First()
			: serviceManager.Get(options.Value.KeyValueStoreName);
	}

	public async Task<long> Append(byte[] data,
		IDictionary<string, string>? metaData = default,
		TimeSpan? timeToLive = default)
	{
		long eventId = ++_eventId;
		_dirty = true;
		EventSourceEvent eventSourceEvent = new(eventId, DateTimeOffset.UtcNow, data, metaData);
		byte[] serializedData = _serializationManager.SerializeToByteArray(eventSourceEvent);
		await WriteBytes(serializedData, eventId, timeToLive).ConfigureAwait(true);
		return eventId;
	}

	public Task<long> GetNextEventId()
	{
		return Task.FromResult(_eventId + 1);
	}

	public override async Task OnActivateAsync()
	{
		await base.OnActivateAsync().ConfigureAwait(true);

		_eventId = await ReadEventState().ConfigureAwait(true);

		byte[]? bytes;
		if (_eventId < 0)
		{
			bytes = await ReadBytes(0).ConfigureAwait(true);
		}
		else
		{
			bytes = await ReadBytes(_eventId).ConfigureAwait(true);
		}

		while (bytes is not null)
		{
			bytes = await ReadBytes(_eventId + 1).ConfigureAwait(true);
			if (bytes is not null)
			{
				_eventId++;
				_dirty = true;
			}
		}

		_timerRegistration = RegisterTimer(_ => WriteEventState(),
			default,
			_options.Value.GrainSnapshotPersistenceInterval,
			_options.Value.GrainSnapshotPersistenceInterval);
	}

	public override async Task OnDeactivateAsync()
	{
		_timerRegistration?.Dispose();
		await WriteEventState().ConfigureAwait(true);
		await base.OnDeactivateAsync().ConfigureAwait(true);
	}

	private async Task<long> ReadEventState()
	{
		byte[]? eventIdBytes = await ReadBytes(-1).ConfigureAwait(true);
		if (eventIdBytes is null || eventIdBytes.Length != sizeof(long))
		{
			return -1;
		}

		long eventId = BitConverter.ToInt64(eventIdBytes);
		if (eventId < 0)
		{
			return -1;
		}

		return eventId;
	}

	private async Task WriteEventState()
	{
		if (!_dirty || _eventId < 0)
		{
			return;
		}

		byte[] eventIdBytes = BitConverter.GetBytes(_eventId);
		await WriteBytes(eventIdBytes, -1, _options.Value.GrainStateTimeToLive).ConfigureAwait(true);

		_dirty = false;
	}

	private async ValueTask WriteBytes(byte[] data, long eventId, TimeSpan? timeToLive)
	{
		string key = DefaultEventStore.GetStoreKey(this.GetPrimaryKeyString(), eventId);
		await _keyValueStore.Set(key, data, default, timeToLive).ConfigureAwait(true);
	}

	private ValueTask<byte[]?> ReadBytes(long eventId)
	{
		string key = DefaultEventStore.GetStoreKey(this.GetPrimaryKeyString(), eventId);
		return _keyValueStore.GetValue(key);
	}
}
