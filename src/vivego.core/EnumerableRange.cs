using System;
using System.Collections;
using System.Collections.Generic;

namespace vivego.core
{
	public sealed class EnumerableRange : IEnumerable<long>
	{
		private readonly long _start;
		private readonly long _end;

		public EnumerableRange(Range range) : this(range.Start.Value, range.End.Value)
		{
		}

		public EnumerableRange(long start, long end)
		{
			_start = start;
			_end = end;
		}

		public IEnumerator<long> GetEnumerator()
		{
			if (_start < _end)
			{
				for (long index = _start; index <= _end; index++)
				{
					yield return index;
				}

				yield break;
			}

			for (long index = _end; index >= _start; index--)
			{
				yield return index;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static implicit operator EnumerableRange(Range range) => ToEnumerableRange(range);

		public static implicit operator EnumerableRange((long start, long end) tuple) => ToEnumerableRange(tuple);

		public static EnumerableRange ToEnumerableRange(Range range) => new(range);

		public static EnumerableRange ToEnumerableRange((long start, long end) tuple) => new(tuple.start, tuple.end);
	}
}
