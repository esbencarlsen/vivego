using System;
using System.Text;
using System.Threading.Tasks;

using Google.Protobuf;

using Microsoft.Extensions.Options;

using Proto.Persistence;

using vivego.Serializer;
using vivego.Serializer.Model;

namespace vivego.KeyValue.ProtoActorPersistence
{
	public sealed class KeyValueStoreProtoActorProvider : IProvider
	{
		private readonly IOptions<KeyValueStoreProtoActorProviderOptions> _options;
		private readonly ISerializer _serializer;
		private readonly IKeyValueStore _keyValueStore;

		public KeyValueStoreProtoActorProvider(
			IOptions<KeyValueStoreProtoActorProviderOptions> options,
			ISerializer serializer,
			IKeyValueStore keyValueStore)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<long> GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
		{
			if (callback is null) throw new ArgumentNullException(nameof(callback));
			if (string.IsNullOrEmpty(actorName)) throw new ArgumentException("Value cannot be null or empty.", nameof(actorName));

			for (long index = indexStart; index < indexEnd; index++)
			{
				byte[]? serializedData = await _keyValueStore
					.GetValue(actorName + index)
					.ConfigureAwait(false);

				if (serializedData is null)
				{
					return index - 1;
				}

				object? o = await _serializer
					.Deserialize<object>(SerializedValue.Parser.ParseFrom(serializedData))
					.ConfigureAwait(false);

				if (o is null)
				{
					return index - 1;
				}

				callback(o);
			}

			return indexEnd - 1; // Not used
		}

		public async Task<long> PersistEventAsync(string actorName, long index, object @event)
		{
			SerializedValue serializedData = await _serializer
				.Serialize(@event)
				.ConfigureAwait(false);
			await _keyValueStore
				.Set(actorName + index, serializedData.ToByteArray(), default, _options.Value.TimeToLive)
				.ConfigureAwait(false);
			return -1; // Not used
		}

		public async Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
		{
			for (long index = inclusiveToIndex; index >= 0; index--)
			{
				await _keyValueStore
					.DeleteEntry(actorName + index)
					.ConfigureAwait(false);
			}
		}

		public async Task<(object? Snapshot, long Index)> GetSnapshotAsync(string actorName)
		{
			byte[]? serializedSnapshot = await _keyValueStore
				.GetValue(GetSnapshotName(actorName))
				.ConfigureAwait(false);

			if (serializedSnapshot is null)
			{
				return (default, -1);
			}

			SerializedValue serializedData = SerializedValue.Parser.ParseFrom(serializedSnapshot);
			if (!serializedData.Data.TryGetValue("index", out ByteString? indexByteString)
				|| indexByteString is null)
			{
				return (default, -1);
			}

			long index = BitConverter.ToInt64(indexByteString.ToByteArray());

			object? deserialized = await _serializer
				.Deserialize(serializedData)
				.ConfigureAwait(false);

			return (deserialized, index);
		}

		public async Task PersistSnapshotAsync(string actorName, long index, object snapshot)
		{
			SerializedValue serializedData = await _serializer
				.Serialize(snapshot)
				.ConfigureAwait(false);
			serializedData.Data["actorName"] = ByteString.CopyFrom(actorName, Encoding.UTF8);
			serializedData.Data["index"] = ByteString.CopyFrom(BitConverter.GetBytes(index));
			await _keyValueStore
				.Set(GetSnapshotName(actorName), serializedData.ToByteArray(), default, _options.Value.TimeToLive)
				.ConfigureAwait(false);
		}

		public async Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
		{
			await _keyValueStore
				.DeleteEntry(GetSnapshotName(actorName))
				.ConfigureAwait(false);
		}

		private static string GetSnapshotName(string actorName)
		{
			return $"snapshot_{actorName}";
		}
	}
}
