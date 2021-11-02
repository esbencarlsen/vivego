using System;

namespace vivego.Collection.Index
{
	public sealed class TimerIndexCompactionStrategy : IIndexCompactionStrategy
	{
		private DateTimeOffset _lastCompaction;
		private readonly TimeSpan _compactEvery;

		public TimerIndexCompactionStrategy(TimeSpan compactEvery)
		{
			if (compactEvery <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(compactEvery));
			_compactEvery = compactEvery;
			_lastCompaction = DateTimeOffset.UtcNow;
		}

		public bool DoCompaction(long version)
		{
			if (DateTimeOffset.UtcNow - _lastCompaction > _compactEvery)
			{
				_lastCompaction = DateTimeOffset.UtcNow;
				return true;
			}

			return false;
		}
	}
}
