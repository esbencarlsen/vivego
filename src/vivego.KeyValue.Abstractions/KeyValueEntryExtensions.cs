using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue;

public static class KeyValueEntryExtensions
{
	public static readonly KeyValueEntry KeyValueNull = new()
	{
		Value = NullableBytesExtensions.EmptyNullableBytes
	};

	public static ValueTask<string> Set(this IKeyValueStore keyValueStore,
		string key,
		byte[]? value,
		string? etag = default,
		TimeSpan? timeToLive = default,
		CancellationToken cancellationToken = default)
	{
		if (keyValueStore is null) throw new ArgumentNullException(nameof(keyValueStore));
		if (key is null) throw new ArgumentNullException(nameof(key));

		return keyValueStore.Set(new SetKeyValueEntry
		{
			Key = key,
			ETag = etag ?? string.Empty,
			ExpiresInSeconds = (int)timeToLive.GetValueOrDefault(TimeSpan.Zero).TotalSeconds,
			Value = value.ToNullableBytes()
		}, cancellationToken);
	}

	public static ValueTask<string> SetStringValue(this IKeyValueStore keyValueStore,
		string key,
		string value,
		string? etag = default,
		TimeSpan? timeToLive = default,
		CancellationToken cancellationToken = default)
	{
		if (keyValueStore is null) throw new ArgumentNullException(nameof(keyValueStore));
		if (key is null) throw new ArgumentNullException(nameof(key));

		byte[] valueBytes = Encoding.UTF8.GetBytes(value);
		return keyValueStore.Set(new SetKeyValueEntry
		{
			Key = key,
			ETag = etag ?? string.Empty,
			ExpiresInSeconds = (int)timeToLive.GetValueOrDefault(TimeSpan.Zero).TotalSeconds,
			Value = valueBytes.ToNullableBytes()
		}, cancellationToken);
	}

	public static async ValueTask<byte[]?> GetValue(this IKeyValueStore keyValueStore,
		string key,
		CancellationToken cancellationToken = default)
	{
		if (keyValueStore is null) throw new ArgumentNullException(nameof(keyValueStore));
		if (key is null) throw new ArgumentNullException(nameof(key));

		KeyValueEntry keyValueEntry = await keyValueStore
			.Get(key, cancellationToken)
			.ConfigureAwait(false);
		return keyValueEntry.Value.ToBytes();
	}

	public static async Task<string?> GetValueString(this IKeyValueStore keyValueStore,
		string key,
		CancellationToken cancellationToken = default)
	{
		if (keyValueStore is null) throw new ArgumentNullException(nameof(keyValueStore));
		if (key is null) throw new ArgumentNullException(nameof(key));

		KeyValueEntry keyValueEntry = await keyValueStore
			.Get(key, cancellationToken)
			.ConfigureAwait(false);
		byte[]? valueBytes = keyValueEntry.Value.ToBytes();
		if (valueBytes is null)
		{
			return default;
		}

		string value = Encoding.UTF8.GetString(valueBytes);
		return value;
	}

	public static ValueTask<bool> DeleteEntry(this IKeyValueStore keyValueStore,
		string key,
		CancellationToken cancellationToken)
	{
		if (keyValueStore is null) throw new ArgumentNullException(nameof(keyValueStore));
		if (key is null) throw new ArgumentNullException(nameof(key));

		return keyValueStore
			.Delete(new DeleteKeyValueEntry
			{
				Key = key,
				ETag = string.Empty
			}, cancellationToken);
	}

	public static ValueTask<bool> DeleteEntry(this IKeyValueStore keyValueStore,
		string key,
		string? etag = default,
		CancellationToken cancellationToken = default)
	{
		if (keyValueStore is null) throw new ArgumentNullException(nameof(keyValueStore));
		if (key is null) throw new ArgumentNullException(nameof(key));

		return keyValueStore
			.Delete(new DeleteKeyValueEntry
			{
				Key = key,
				ETag = etag ?? string.Empty
			}, cancellationToken);
	}
}
