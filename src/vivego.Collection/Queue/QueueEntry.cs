namespace vivego.Collection.Queue
{
	internal sealed record QueueEntry : IQueueEntry
	{
		public long Version { get; }
		public string Id { get; }
		public Value Data { get; }

		public QueueEntry(long version, string id, Value data)
		{
			Version = version;
			Id = id;
			Data = data;
		}
	}
}