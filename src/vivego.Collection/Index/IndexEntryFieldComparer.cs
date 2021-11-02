using System;
using System.Collections.Generic;
using vivego.core;

namespace vivego.Collection.Index
{
    public sealed class IndexEntryFieldComparer : IComparer<IIndexEntry>, IEqualityComparer<IIndexEntry>
    {
        private readonly IComparer<byte[]> _byteComparer;

        public IndexEntryFieldComparer(IComparer<byte[]> byteComparer)
        {
            _byteComparer = byteComparer ?? throw new ArgumentNullException(nameof(byteComparer));
        }

        public int Compare(IIndexEntry? x, IIndexEntry? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            return _byteComparer.Compare(x.Field.Data, y.Field.Data);
        }

        public bool Equals(IIndexEntry? x, IIndexEntry? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Field.Data.AsSpan().SequenceEqual(y.Field.Data.AsSpan());
        }

        public int GetHashCode(IIndexEntry obj)
        {
            if (obj is null) return 0;
            return obj.Field.Data.GetDeterministicHashCode();
        }
    }
}
