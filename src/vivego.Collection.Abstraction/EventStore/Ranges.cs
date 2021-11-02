using vivego.EventStore;

namespace vivego.Collection.EventStore
{
	public static class Ranges
	{
		public static vivego.EventStore.Range All { get; } = new()
		{
			Start = 0,
			End = -1
		};
	}
}
