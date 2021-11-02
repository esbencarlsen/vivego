namespace vivego.KeyValue;

public sealed record SetKeyValueEntry<T>(string Key,
	T? Value,
	long ExpiresInSeconds,
	string ETag) where T : notnull;
