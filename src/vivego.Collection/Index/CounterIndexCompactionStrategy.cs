using System;

namespace vivego.Collection.Index
{
	public sealed class CounterIndexCompactionStrategy : IIndexCompactionStrategy
	{
		private long _counter;
		private readonly long _compactEvery;

		public CounterIndexCompactionStrategy(long compactEvery)
		{
			if (compactEvery <= 0) throw new ArgumentOutOfRangeException(nameof(compactEvery));
			_compactEvery = compactEvery;
		}

		public bool DoCompaction(long version)
		{
			return ++_counter % _compactEvery == 0;
		}
	}
}
