namespace vivego.Collection.Index
{
	public interface IIndexCompactionStrategy
	{
		bool DoCompaction(long version);
	}
}