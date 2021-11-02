using System;

namespace vivego.KeyValue.Sql
{
	public sealed class StateEntry
	{
		public string Id { get; set; } = string.Empty;
		public byte[] Data { get; set; } = Array.Empty<byte>();
		public string Etag { get; set; } = string.Empty;
		public DateTimeOffset? ExpiresAt { get; set; }
	}
}
