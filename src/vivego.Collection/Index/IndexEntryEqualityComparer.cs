using System;
using System.Collections.Generic;
using vivego.core;

namespace vivego.Collection.Index
{
	internal class IndexEntryEqualityComparer : IEqualityComparer<IIndexEntry>
	{
		public static readonly IndexEntryEqualityComparer Instance = new();

		public bool Equals(IIndexEntry? x, IIndexEntry? y)
		{
			if (ReferenceEquals(x, y)) return true;
			if (x is null) return false;
			if (y is null) return false;
			if (x.GetType() != y.GetType()) return false;

			return x.Field.Data.AsSpan().SequenceEqual(y.Field.Data.AsSpan());
		}

		public int GetHashCode(IIndexEntry obj)
		{
			return obj.Field.Data.GetDeterministicHashCode();
		}
	}
}