using System;

namespace vivego.MessageBroker.EventStore;

public sealed class EventStoreOptions
{
	public string? KeyValueStoreName { get; set; } = "Default";
	public TimeSpan? GrainStateTimeToLive { get; set; }
	public TimeSpan GrainSnapshotPersistenceInterval { get; set; } = TimeSpan.FromMinutes(1);
	public TimeSpan EventSourceItemGetCacheTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
