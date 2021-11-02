namespace vivego.KeyValue;

public sealed class KeyValueEntry<T> where T : notnull
{
	public T? Value { get; }
	public long ExpiresAtUnixTimeSeconds { get; }
	public string ETag { get; }

	public KeyValueEntry(T? value, long expiresAtUnixTimeSeconds, string eTag)
	{
		Value = value;
		ExpiresAtUnixTimeSeconds = expiresAtUnixTimeSeconds;
		ETag = eTag;
	}
}
