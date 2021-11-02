using System;
using System.Security.Cryptography;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue;

public static class SetKeyValueEntryExtensions
{
	public static KeyValueEntry ConvertToKeyValueEntry(this SetKeyValueEntry setKeyValueEntry, bool skipETag = false)
	{
		if (setKeyValueEntry is null) throw new ArgumentNullException(nameof(setKeyValueEntry));
		KeyValueEntry keyValueEntry = new()
		{
			Value = setKeyValueEntry.Value,
			ETag = skipETag ? string.Empty : setKeyValueEntry.CalculateChecksum()
		};
		if (setKeyValueEntry.ExpiresInSeconds > 0)
		{
			DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddSeconds(setKeyValueEntry.ExpiresInSeconds);
			keyValueEntry.ExpiresAtUnixTimeSeconds = expiresAt.ToUnixTimeSeconds();
		}

		return keyValueEntry;
	}

	public static string CalculateChecksum(this SetKeyValueEntry setKeyValueEntry)
	{
		if (setKeyValueEntry is null) throw new ArgumentNullException(nameof(setKeyValueEntry));

		if (setKeyValueEntry.Value.KindCase is NullableBytes.KindOneofCase.Data)
		{
			using HashAlgorithm hashAlgorithm = SHA384.Create();
			byte[] bytes = hashAlgorithm.ComputeHash(setKeyValueEntry.Value.ToBytes() ?? Array.Empty<byte>());
			string checksum = Convert.ToBase64String(bytes);
			return checksum;
		}

		return string.Empty;
	}
}
