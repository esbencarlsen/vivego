using System;
using System.Collections.Generic;

namespace vivego.Collection.Index
{
	internal class DescendingIndexComparer : IComparer<IIndexEntry>
	{
		public static readonly DescendingIndexComparer Instance = new();
		
		public int Compare(IIndexEntry? x, IIndexEntry? y)
		{
			if (x is null) throw new ArgumentNullException(nameof(x));
			if (y is null) throw new ArgumentNullException(nameof(y));
			int result = x.Field.Data.AsSpan().SequenceCompareTo(y.Field.Data.AsSpan());
			return result switch
			{
				< 0 => 1,
				> 0 => -1,
				_ => result
			};
		}
	}
}