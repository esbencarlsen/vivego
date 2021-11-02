namespace vivego.Collection.Index
{
	public interface IIndexEntry
	{
		Value Field { get; }
		Value? Data { get; }
	}
}