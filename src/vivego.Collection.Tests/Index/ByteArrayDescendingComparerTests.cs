using System;
using System.Collections.Generic;
using System.Linq;

using vivego.Collection.Index;

using Xunit;

namespace vivego.Collection.Tests.Index
{
	public sealed class ByteArrayDescendingComparerTests
	{
		[Fact]
		public void TestSortOrder()
		{
			SortedList<byte[], byte[]> sortedList = new(ByteArrayDescendingComparer.Instance)
			{
				{ new Value("A").Data, Array.Empty<byte>() },
				{ new Value("B").Data, Array.Empty<byte>() },
				{ new Value("C").Data, Array.Empty<byte>() }
			};

			KeyValuePair<byte[], byte[]>[] all = sortedList.ToArray();

			Assert.NotNull(all);
			Assert.NotEmpty(all);
			Assert.Equal(3, all.Length);

			Assert.Equal("A", ((Value)all[2].Key).AsString);
			Assert.Equal("B", ((Value)all[1].Key).AsString);
			Assert.Equal("C", ((Value)all[0].Key).AsString);
		}
	}
}
