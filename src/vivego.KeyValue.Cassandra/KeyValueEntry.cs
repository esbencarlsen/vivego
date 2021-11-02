using System;

namespace vivego.KeyValue.Cassandra;

public sealed class KeyValueEntry
{
	public string Id { get; set; } = null!;
	public byte[]? Data { get; set; }
	public string? ETag { get; set; }
	public DateTimeOffset? ExpiresAt { get; set; }
}
