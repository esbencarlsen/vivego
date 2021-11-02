using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.Queue;
using vivego.KeyValue;

using Xunit;
using Xunit.Abstractions;

namespace vivego.Collection.Tests.Queue
{
	public sealed class QueueTests
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public QueueTests(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
		}

		private static IQueue GetQueue()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddMemoryCache();
			serviceCollection.AddInMemoryKeyValueStore();
			serviceCollection.AddLogging();
			serviceCollection.AddQueue();
			IQueue queue = serviceCollection.BuildServiceProvider().GetRequiredService<IQueue>();
			return queue;
		}

		[Fact]
		public void CanCreateQueue()
		{
			IQueue queue = GetQueue();
			Assert.NotNull(queue);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanAppend()
		{
			IQueue queue = GetQueue();
			long? version = await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			Assert.NotNull(version);
			IQueueEntry? result = await queue.Get("A", version!.Value).ConfigureAwait(false);
			Assert.NotNull(result);
			Assert.Empty(result!.Data.Data);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanAppendExpectedVersionOnEmpty()
		{
			IQueue queue = GetQueue();
			long? version = await queue.Append("A", Array.Empty<byte>(), 0).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(0, version!.Value);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanAppendExpectedVersion2X()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>(), 0).ConfigureAwait(false);
			long? version = await queue.Append("A", Array.Empty<byte>(), 1).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(1, version!.Value);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanAppendTakeLastAppendExpectedVersion()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.TryTakeLast("A").ConfigureAwait(false);
			long? version = await queue.Append("A", Array.Empty<byte>(), 0).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(0, version!.Value);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanAppendTakeFirstAppendExpectedVersion()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.TryTakeFirst("A").ConfigureAwait(false);
			long? version = await queue.Append("A", Array.Empty<byte>(), 1).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(1, version!.Value);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanPrepend()
		{
			IQueue queue = GetQueue();
			long? version = await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			Assert.NotNull(version);
			IQueueEntry? result = await queue.Get("A", version!.Value).ConfigureAwait(false);
			Assert.NotNull(result);
			Assert.Empty(result!.Data.Data);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanPrependExpectedVersion()
		{
			IQueue queue = GetQueue();
			long? version = await queue.Prepend("A", Array.Empty<byte>(), -1).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(-1, version!.Value);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanPrependExpectedVersion2X()
		{
			IQueue queue = GetQueue();
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			long? version = await queue.Prepend("A", Array.Empty<byte>(), -2).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(-2, version!.Value);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanAppendAndPrepend()
		{
			IQueue queue = GetQueue();
			long? appendVersion = await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			long? prependVersion = await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			Assert.NotNull(appendVersion);
			Assert.NotNull(prependVersion);
			IQueueEntry? appendResult = await queue.Get("A", appendVersion!.Value).ConfigureAwait(false);
			IQueueEntry? prependResult = await queue.Get("A", prependVersion!.Value).ConfigureAwait(false);
			Assert.NotNull(appendResult);
			Assert.Empty(appendResult!.Data.Data);
			Assert.NotNull(prependResult);
			Assert.Empty(prependResult!.Data.Data);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanCount()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			long count = await queue.Count("A").ConfigureAwait(false);
			Assert.Equal(2, count);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanTruncateAll()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Truncate("A").ConfigureAwait(false);
			long count = await queue.Count("A").ConfigureAwait(false);
			Assert.Equal(0, count);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanTruncateS1()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Truncate("A", -1, 1).ConfigureAwait(false);
			long count = await queue.Count("A").ConfigureAwait(false);
			Assert.Equal(2, count);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanGetAll()
		{
			IQueue queue = GetQueue();
			string queueName = Guid.NewGuid().ToString();
			await queue.Append(queueName, Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend(queueName, Array.Empty<byte>()).ConfigureAwait(false);
			IQueueEntry[]? all = await queue.GetAll(queueName).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.NotEmpty(all);
			Assert.Equal(2, all.Length);
			Assert.Equal(-1, all[0].Version);
			Assert.Equal(queueName, all[0].Id);
			Assert.Equal(0, all[1].Version);
			Assert.Equal(queueName, all[1].Id);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanGetAllReverse()
		{
			IQueue queue = GetQueue();
			string queueName = Guid.NewGuid().ToString();
			await queue.Append(queueName, Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend(queueName, Array.Empty<byte>()).ConfigureAwait(false);
			IQueueEntry[]? all = await queue.GetAllReverse(queueName).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.NotEmpty(all);
			Assert.Equal(2, all.Length);
			Assert.Equal(0, all[0].Version);
			Assert.Equal(queueName, all[0].Id);
			Assert.Equal(-1, all[1].Version);
			Assert.Equal(queueName, all[1].Id);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanGetAllWithSkip()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			IQueueEntry[]? all = await queue.GetAll("A", 3).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.NotEmpty(all);
			Assert.Equal(3, all.Length);
		}

		[Fact]
		public async System.Threading.Tasks.Task CanGetAllReverseWithSkip()
		{
			IQueue queue = GetQueue();
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			await queue.Prepend("A", Array.Empty<byte>()).ConfigureAwait(false);
			IQueueEntry[]? all = await queue.GetAllReverse("A", 3).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.NotEmpty(all);
			Assert.Equal(3, all.Length);
		}

		[Fact]
		public async System.Threading.Tasks.Task PerformanceTest()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection
				.AddMemoryCache()
				.AddInMemoryKeyValueStore()
				.AddStateCachePipelineBehavior();
			serviceCollection.AddLogging();
			serviceCollection.AddQueue();
			IQueue queue = serviceCollection.BuildServiceProvider().GetRequiredService<IQueue>();

			Stopwatch stopwatch = Stopwatch.StartNew();
			foreach (int unused in Enumerable.Range(0, 10000))
			{
				await queue.Append("A", Array.Empty<byte>()).ConfigureAwait(false);
			}

			stopwatch.Stop();

			_testOutputHelper.WriteLine(stopwatch.Elapsed.ToString());
		}
	}
}
