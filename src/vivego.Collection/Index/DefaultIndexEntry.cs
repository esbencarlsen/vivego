namespace vivego.Collection.Index
{
	public sealed record DefaultIndexEntry : IIndexEntry
	{
		public Value Field { get; }
		public Value? Data { get; }

		public DefaultIndexEntry(Value field, Value? data)
		{
			Field = field;
			Data = data;
		}
	}
}