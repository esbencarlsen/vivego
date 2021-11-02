using System;
using System.Linq;

using vivego.Collection.Index;

using Xunit;

namespace vivego.Collection.Tests.Index
{
	public sealed class ByteArrayAscendingComparerTests
	{
		[Fact]
		public void TestSortOrderString()
		{
			byte[][] array =
			{
				new Value("C").Data,
				new Value("B").Data,
				new Value("A").Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.True(((Value)array[0]).Equals(sortedArray[0]));
			Assert.True(((Value)array[1]).Equals(sortedArray[1]));
			Assert.True(((Value)array[2]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderStringFailing()
		{
			byte[][] array =
			{
				new Value("C").Data,
				new Value("B").Data,
				new Value("A").Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.False(((Value)array[2]).Equals(sortedArray[0]));
			Assert.False(((Value)array[0]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderDateTime()
		{
			DateTime now = DateTime.UtcNow;
			byte[][] array =
			{
				new Value(now.AddTicks(3)).Data,
				new Value(now.AddTicks(2)).Data,
				new Value(now.AddTicks(1)).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.True(((Value)array[0]).Equals(sortedArray[0]));
			Assert.True(((Value)array[1]).Equals(sortedArray[1]));
			Assert.True(((Value)array[2]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderDateTimeFailing()
		{
			DateTime now = DateTime.UtcNow;
			byte[][] array =
			{
				new Value(now.AddTicks(3)).Data,
				new Value(now.AddTicks(2)).Data,
				new Value(now.AddTicks(1)).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.False(((Value)array[2]).Equals(sortedArray[0]));
			Assert.False(((Value)array[1]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderDateTimeOffSet()
		{
			DateTimeOffset now = DateTimeOffset.UtcNow;
			byte[][] array =
			{
				new Value(now.AddTicks(3)).Data,
				new Value(now.AddTicks(2)).Data,
				new Value(now.AddTicks(1)).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.True(((Value)array[0]).Equals(sortedArray[0]));
			Assert.True(((Value)array[1]).Equals(sortedArray[1]));
			Assert.True(((Value)array[2]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderDateTimeOffSetFailing()
		{
			DateTimeOffset now = DateTimeOffset.UtcNow;
			byte[][] array =
			{
				new Value(now.AddTicks(3)).Data,
				new Value(now.AddTicks(2)).Data,
				new Value(now.AddTicks(1)).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.False(((Value)array[2]).Equals(sortedArray[0]));
			Assert.False(((Value)array[1]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderGuid()
		{
			byte[][] array =
			{
				new Value(Guid.Parse("5E071F67-E955-4572-83D5-4EF8AC4DFF32")).Data,
				new Value(Guid.Parse("5E071F67-E955-4572-83D5-4EF8AC4DFF31")).Data,
				new Value(Guid.Parse("5E071F67-E955-4572-83D5-4EF8AC4DFF30")).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.True(((Value)array[0]).Equals(sortedArray[0]));
			Assert.True(((Value)array[1]).Equals(sortedArray[1]));
			Assert.True(((Value)array[2]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderGuidFailing()
		{
			byte[][] array =
			{
				new Value(Guid.Parse("5E071F67-E955-4572-83D5-4EF8AC4DFF32")).Data,
				new Value(Guid.Parse("5E071F67-E955-4572-83D5-4EF8AC4DFF31")).Data,
				new Value(Guid.Parse("5E071F67-E955-4572-83D5-4EF8AC4DFF30")).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.False(((Value)array[2]).Equals(sortedArray[0]));
			Assert.False(((Value)array[0]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderInteger()
		{
			byte[][] array =
			{
				new Value(3).Data,
				new Value(2).Data,
				new Value(1).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.True(((Value)array[0]).Equals(sortedArray[0]));
			Assert.True(((Value)array[1]).Equals(sortedArray[1]));
			Assert.True(((Value)array[2]).Equals(sortedArray[2]));
		}

		[Fact]
		public void TestSortOrderIntegerFailing()
		{
			byte[][] array =
			{
				new Value(3).Data,
				new Value(2).Data,
				new Value(1).Data
			};

			byte[][] sortedArray = array
				.OrderBy(a => a, ByteArrayAscendingComparer.Instance)
				.ToArray();
			Array.Sort(array, ByteArrayAscendingComparer.Instance);

			Assert.NotNull(sortedArray);
			Assert.NotEmpty(sortedArray);
			Assert.Equal(3, sortedArray.Length);

			Assert.False(((Value)array[2]).Equals(sortedArray[0]));
			Assert.False(((Value)array[0]).Equals(sortedArray[2]));
		}
	}
}
