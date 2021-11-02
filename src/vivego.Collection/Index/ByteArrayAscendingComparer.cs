using System;
using System.Collections.Generic;

namespace vivego.Collection.Index
{
	public sealed class ByteArrayAscendingComparer : IComparer<byte[]>
	{
		public static readonly IComparer<byte[]> Instance = new ByteArrayAscendingComparer();

		public int Compare(byte[]? x, byte[]? y)
		{
			if (x is null) throw new ArgumentNullException(nameof(x));
			if (y is null) throw new ArgumentNullException(nameof(y));
			return x.AsSpan().SequenceCompareTo(y.AsSpan());
		}
	}
}
