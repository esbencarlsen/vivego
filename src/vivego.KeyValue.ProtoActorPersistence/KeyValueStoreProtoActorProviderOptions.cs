using System;

namespace vivego.KeyValue.ProtoActorPersistence
{
	public sealed record KeyValueStoreProtoActorProviderOptions
	{
		public TimeSpan? TimeToLive { get; set; }
	}
}
