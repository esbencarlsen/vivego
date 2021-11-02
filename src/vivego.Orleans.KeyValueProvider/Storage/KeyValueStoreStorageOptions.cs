using System;

namespace vivego.Orleans.KeyValueProvider.Storage
{
	public sealed class KeyValueStoreStorageOptions
	{
		public TimeSpan? Ttl { get; set; }
		public bool SupportsETag { get; set; }
	}
}
