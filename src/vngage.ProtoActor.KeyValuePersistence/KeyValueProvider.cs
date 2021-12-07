using Google.Protobuf;

using Proto.Persistence;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.Serializer;
using vivego.Serializer.Model;

namespace vngage.ProtoActor.KeyValuePersistence;

public sealed class KeyValueProvider : IProvider
{
	private readonly ISerializer _serializer;
	private readonly IKeyValueStore _keyValueStore;

	public KeyValueProvider(
		ISerializer serializer,
		IKeyValueStore keyValueStore)
	{
		_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
	}

	private static string MakeKey(string actorName, string type, long index)
	{
		return $"{actorName}_{type}_{index}";
	}

	private static string MakeEventKey(string actorName, long index)
	{
		return MakeKey(actorName, "Event", index);
	}

	private static string MakeSnapshotKey(string actorName)
	{
		return MakeKey(actorName, "Snapshot", 0);
	}

	public async Task<long> GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
	{
		if (callback is null) throw new ArgumentNullException(nameof(callback));
		long lastIndex = -1;
		for (long index = indexStart; index <= indexEnd; index++)
		{
			string key = MakeEventKey(actorName, index);
			(object? data, long i) = await Get(key).ConfigureAwait(false);
			if (data is null)
			{
				break;
			}

			callback(data);
			lastIndex = i;
		}

		return lastIndex;
	}

	public async Task<long> PersistEventAsync(string actorName, long index, object @event)
	{
		string key = MakeEventKey(actorName, index);
		await Persist(key, index, @event).ConfigureAwait(false);
		return index + 1;
	}

	public async Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
	{
		for (int index = 0; index <= inclusiveToIndex; index++)
		{
			string key = MakeEventKey(actorName, inclusiveToIndex);
			await _keyValueStore.DeleteEntry(key).ConfigureAwait(false);
		}
	}

	public Task<(object? Snapshot, long Index)> GetSnapshotAsync(string actorName)
	{
		string key = MakeSnapshotKey(actorName);
		return Get(key);
	}

	public Task PersistSnapshotAsync(string actorName, long index, object snapshot)
	{
		string key = MakeSnapshotKey(actorName);
		return Persist(key, index, snapshot);
	}

	public Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
	{
		throw new NotImplementedException();
	}

	public async Task Persist(string key, long index, object data)
	{
		SerializedValue serialized = await _serializer
			.Serialize(data)
			.ConfigureAwait(false);
		SetKeyValueEntry setKeyValueEntry = new()
		{
			Key = key,
			Value = serialized.ToNullableBytes()
		};
		setKeyValueEntry.MetaData.Add("EventIndex", ByteString.CopyFrom(BitConverter.GetBytes(index)));
		await _keyValueStore
			.Set(setKeyValueEntry)
			.ConfigureAwait(false);
	}

	public async Task<(object? Data, long Index)> Get(string key)
	{
		KeyValueEntry keyValueEntry = await _keyValueStore.Get(key, CancellationToken.None).ConfigureAwait(false);
		if (keyValueEntry.Value.IsNull())
		{
			return (default, -1);
		}

		if (!keyValueEntry.MetaData.TryGetValue("EventIndex", out ByteString? eventIndexByteString))
		{
			return (default, -1);
		}

		long index = BitConverter.ToInt64(eventIndexByteString.Span);
		object? o = await _serializer
			.Deserialize(SerializedValue.Parser.ParseFrom(keyValueEntry.Value.Data))
			.ConfigureAwait(false);
		if (o is null)
		{
			return (default, -1);
		}

		return (o, index);
	}
}
