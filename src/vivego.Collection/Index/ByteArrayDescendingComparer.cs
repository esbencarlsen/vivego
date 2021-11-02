using System;
using System.Collections.Generic;

namespace vivego.Collection.Index
{
	public sealed class ByteArrayDescendingComparer : IComparer<byte[]>
	{
		public static readonly IComparer<byte[]> Instance = new ByteArrayDescendingComparer();

		public int Compare(byte[]? x, byte[]? y)
		{
			if (x is null) throw new ArgumentNullException(nameof(x));
			if (y is null) throw new ArgumentNullException(nameof(y));
			int result = x.AsSpan().SequenceCompareTo(y.AsSpan());
			return result switch
			{
				< 0 => 1,
				> 0 => -1,
				_ => result
			};
		}
	}
}
