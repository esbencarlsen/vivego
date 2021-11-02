using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using vivego.Collection.Index;
using vivego.Collection.Queue;
using vivego.KeyValue;

using Xunit;

namespace vivego.Collection.Tests.Index
{
#pragma warning disable CA2000
	public sealed class IndexTests
	{
		private readonly ServiceProvider _serviceProvider;

		public IndexTests()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddMemoryCache();
			serviceCollection.AddInMemoryKeyValueStore();
			serviceCollection.AddLogging();
			serviceCollection.AddQueue();
			_serviceProvider = serviceCollection.BuildServiceProvider();
		}

		private IIndex GetAscendingOrderedIndex()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			return new DefaultIndex(
				ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(10));
		}

		private IIndex GetDefaultDescendingOrderedIndex()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			return new DefaultIndex(ByteArrayDescendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(10));
		}

		[Fact]
		public void CanCreateIndex()
		{
			GetAscendingOrderedIndex();
		}

		[Fact]
		public async Task CanRemoveWhenEmpty()
		{
			IIndex index = GetAscendingOrderedIndex();
			string indexName = Guid.NewGuid().ToString();
			await index.Remove(indexName, "A").ConfigureAwait(false);
			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(indexName)
				.ConfigureAwait(false);
			Assert.Empty(set);
		}

		[Fact]
		public async Task CanAddAndGet()
		{
			IIndex index = GetAscendingOrderedIndex();
			string key = Guid.NewGuid().ToString();
			DateTimeOffset now = DateTimeOffset.Now;
			foreach (int i in Enumerable.Range(0, 1000).Reverse())
			{
				DateTimeOffset dateTimeOffset = now.AddSeconds(i);
				byte[] data = BitConverter.GetBytes(dateTimeOffset.UtcTicks);
				await index.Add(key, i.ToString(CultureInfo.InvariantCulture), data).ConfigureAwait(false);
			}

			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(key)
				.ConfigureAwait(false);
			IIndexEntry[] all = set.ToArray();
			Assert.Equal(1000, all.Length);
		}

		[Fact]
		public async Task MultipleSortedKeys()
		{
			IIndex index = GetAscendingOrderedIndex();
			string key = Guid.NewGuid().ToString();
			await index.Add(key, "C", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "B", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "A", Array.Empty<byte>()).ConfigureAwait(false);

			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(key)
				.ConfigureAwait(false);
			IIndexEntry[] all = set.ToArray();
			IIndexEntry[] allArray = all.ToArray();
			Assert.NotNull(allArray);
			Assert.Equal(3, allArray.Length);
			Assert.Equal("A", allArray[0].Field.AsString);
			Assert.Equal("B", allArray[1].Field.AsString);
			Assert.Equal("C", allArray[2].Field.AsString);
		}

		[Fact]
		public async Task MultipleSortedKeysDescending()
		{
			IIndex index = GetDefaultDescendingOrderedIndex();
			string key = Guid.NewGuid().ToString();
			await index.Add(key, "A", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "B", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "C", Array.Empty<byte>()).ConfigureAwait(false);

			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(key)
				.ConfigureAwait(false);
			IIndexEntry[] all = set.ToArray();
			IIndexEntry[] allArray = all.ToArray();
			Assert.NotNull(allArray);
			Assert.Equal(3, allArray.Length);
			Assert.Equal("C", allArray[0].Field.AsString);
			Assert.Equal("B", allArray[1].Field.AsString);
			Assert.Equal("A", allArray[2].Field.AsString);
		}

		[Fact]
		public async Task MultipleSortedKeysAfterRemove()
		{
			IIndex index = GetAscendingOrderedIndex();
			string key = Guid.NewGuid().ToString();
			await index.Add(key, "D", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "C", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "B", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key, "A", Array.Empty<byte>()).ConfigureAwait(false);

			await index.Remove(key, "B").ConfigureAwait(false);

			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(key)
				.ConfigureAwait(false);
			IIndexEntry[] all = set.ToArray();
			IIndexEntry[] allArray = all.ToArray();
			Assert.NotNull(allArray);
			Assert.Equal(3, allArray.Length);
			Assert.Equal("A", allArray[0].Field.AsString);
			Assert.Equal("C", allArray[1].Field.AsString);
			Assert.Equal("D", allArray[2].Field.AsString);
		}

		[Fact]
		public async Task SeparateKeys()
		{
			IIndex index = GetAscendingOrderedIndex();
			string key1 = Guid.NewGuid().ToString();
			string key2 = Guid.NewGuid().ToString();
			await index.Add(key1, "A", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(key2, "A", Array.Empty<byte>()).ConfigureAwait(false);

			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(key1)
				.ConfigureAwait(false);
			IIndexEntry[] all = set.ToArray();
			Assert.Single(all);

			set = await index
				.Get(key2)
				.ConfigureAwait(false);
			all = set.ToArray();

			Assert.Single(all);
		}

		[Fact]
		public async Task CanCompact()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			DefaultIndex index = new(ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(long.MaxValue));
			string indexName = Guid.NewGuid().ToString();

			foreach (int i in Enumerable.Range(0, 1000))
			{
				await index.Add(indexName, i, Array.Empty<byte>()).ConfigureAwait(false);
			}

			await index.Compact(indexName).ConfigureAwait(false);

			IQueueEntry[] all = await queue.GetAll(indexName).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Single(all);
			Assert.Equal(1000, all[0].Version);
		}

		[Fact]
		public async Task CanLoadFromDisk()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			DefaultIndex index = new(ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(long.MaxValue));
			string indexName = Guid.NewGuid().ToString();

			await index.Add(indexName, "A", "1").ConfigureAwait(false);
			await index.Add(indexName, "B", "3").ConfigureAwait(false);
			await index.Add(indexName, "C", "3").ConfigureAwait(false);
			await index.Remove(indexName, "B").ConfigureAwait(false);

			index = new DefaultIndex(ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(long.MaxValue));

			ImmutableSortedSet<IIndexEntry> set = await index
				.Get(indexName)
				.ConfigureAwait(false);
			IIndexEntry[] all = set.ToArray();
			Assert.Equal(2, all.Length);
		}

		[Fact]
		public async Task CanContainsEmptyKeys()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			DefaultIndex index = new(ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(long.MaxValue));
			string indexName = Guid.NewGuid().ToString();
			await index.Add(indexName, Array.Empty<byte>(), Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(indexName, Array.Empty<byte>(), Array.Empty<byte>()).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanContainsDuplicateEmptyKeys()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			DefaultIndex index = new(ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(long.MaxValue));
			string indexName = Guid.NewGuid().ToString();
			await index.Add(indexName, Array.Empty<byte>(), Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(indexName, Array.Empty<byte>(), Array.Empty<byte>()).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanContainsDuplicateKeys()
		{
			IQueue queue = _serviceProvider.GetRequiredService<IQueue>();
			DefaultIndex index = new(ByteArrayAscendingComparer.Instance,
				default,
				TimeSpan.FromMinutes(1),
				new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions())),
				queue,
				_ => new CounterIndexCompactionStrategy(long.MaxValue));
			string indexName = Guid.NewGuid().ToString();
			await index.Add(indexName, "A", Array.Empty<byte>()).ConfigureAwait(false);
			await index.Add(indexName, "A", Array.Empty<byte>()).ConfigureAwait(false);
		}
	}
}
