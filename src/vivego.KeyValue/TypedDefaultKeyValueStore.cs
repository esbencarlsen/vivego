using System;
using System.Threading;
using System.Threading.Tasks;

using vivego.KeyValue.Abstractions.Model;
using vivego.Serializer;
using vivego.Serializer.Model;

namespace vivego.KeyValue;

public sealed class TypedDefaultKeyValueStore : ITypedKeyValueStore
{
	private readonly IKeyValueStore _keyValueStore;
	private readonly ISerializer _serializer;

	public TypedDefaultKeyValueStore(
		string name,
		IKeyValueStore keyValueStore,
		ISerializer serializer)
	{
		if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
		Name = name;
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	}

	public string Name { get; }

	public ValueTask<KeyValueStoreFeatures> GetFeatures(CancellationToken cancellationToken = default)
	{
		return _keyValueStore.GetFeatures(cancellationToken);
	}

	public async ValueTask<string> Set<T>(SetKeyValueEntry<T> setKeyValueEntry, CancellationToken cancellationToken = default) where T : notnull
	{
		if (setKeyValueEntry is null) throw new ArgumentNullException(nameof(setKeyValueEntry));

		NullableBytes nullValue;
		if (setKeyValueEntry.Value is null)
		{
			nullValue = NullableBytesExtensions.EmptyNullableBytes;
		}
		else
		{
			SerializedValue serializedValue = await _serializer
				.Serialize(setKeyValueEntry.Value, cancellationToken)
				.ConfigureAwait(false);
			nullValue = serializedValue.ToNullableBytes();
		}

		return await _keyValueStore.Set(new SetKeyValueEntry
			{
				Key = setKeyValueEntry.Key,
				ETag = setKeyValueEntry.ETag,
				ExpiresInSeconds = setKeyValueEntry.ExpiresInSeconds,
				Value = nullValue
			}, cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<KeyValueEntry<T>> Get<T>(string key, CancellationToken cancellationToken = default) where T : notnull
	{
		KeyValueEntry keyValueEntry = await _keyValueStore.Get(key, cancellationToken).ConfigureAwait(false);
		if (keyValueEntry.Value.IsNull())
		{
			return new KeyValueEntry<T>(default, keyValueEntry.ExpiresAtUnixTimeSeconds, keyValueEntry.ETag);
		}

		SerializedValue serializedValue = SerializedValue.Parser.ParseFrom(keyValueEntry.Value.Data);
		T? value = await _serializer
			.Deserialize<T>(serializedValue, cancellationToken)
			.ConfigureAwait(false);
		return new KeyValueEntry<T>(value, keyValueEntry.ExpiresAtUnixTimeSeconds, keyValueEntry.ETag);
	}

	public ValueTask<bool> Delete(DeleteKeyValueEntry deleteKeyValueEntry, CancellationToken cancellationToken = default)
	{
		return _keyValueStore.Delete(deleteKeyValueEntry, cancellationToken);
	}
}
