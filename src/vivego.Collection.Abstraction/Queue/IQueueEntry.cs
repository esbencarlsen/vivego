namespace vivego.Collection.Queue
{
	public interface IQueueEntry
	{
		long Version { get; }
		string Id { get; }
		Value Data { get; }
	}
}