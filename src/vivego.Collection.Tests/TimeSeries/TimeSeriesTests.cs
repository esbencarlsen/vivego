using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.Index;
using vivego.Collection.TimeSeries;
using vivego.KeyValue;

using Xunit;

namespace vivego.Collection.Tests.TimeSeries
{
	public sealed class TimeSeriesTests
	{
		private static ITimeSeries GetTimeSeries()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddInMemoryKeyValueStore();
			serviceCollection.AddQueue();
			serviceCollection.AddLogging();
			serviceCollection.AddMemoryCache();
			serviceCollection.AddTimeSeries(_ => new CounterIndexCompactionStrategy(1000));
			ITimeSeries timeSeries = serviceCollection.BuildServiceProvider().GetRequiredService<ITimeSeries>();
			return timeSeries;
		}

		[Fact]
		public void CanConfigureAndResolveTimeSeries()
		{
			ITimeSeries timeSeries = GetTimeSeries();
			Assert.NotNull(timeSeries);
		}

		[Fact]
		public async Task CanAddEmpty()
		{
			ITimeSeries timeSeries = GetTimeSeries();
			await timeSeries.AddOrUpdate("A", "A", DateTimeOffset.UtcNow, Array.Empty<byte>()).ConfigureAwait(false);
			ITimeSeriesEntry[] all = await timeSeries
				.GetRange("A", DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);

			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanAddSameTwice()
		{
			ITimeSeries timeSeries = GetTimeSeries();
			DateTimeOffset now = DateTimeOffset.UtcNow;
			await timeSeries.AddOrUpdate("A", "A", now, Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "A", now, Array.Empty<byte>()).ConfigureAwait(false);
			ITimeSeriesEntry[]? all = await timeSeries
				.GetRange("A", DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);

			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanGetEmpty()
		{
			ITimeSeries timeSeries = GetTimeSeries();
			ITimeSeriesEntry[]? all = await timeSeries
				.GetRange("A", DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);

			Assert.NotNull(all);
			Assert.Empty(all);
		}

		[Fact]
		public async Task CanGetPartialLower()
		{
			ITimeSeries timeSeries = GetTimeSeries();

			DateTimeOffset now = DateTimeOffset.UtcNow;
			await timeSeries.AddOrUpdate("A", "A", now, Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "B", now.AddDays(1), Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "C", now.AddDays(2), Array.Empty<byte>()).ConfigureAwait(false);

			ITimeSeriesEntry[]? all = await timeSeries
				.GetRange("A", DateTimeOffset.MinValue, now.AddTicks(1))
				.ToArrayAsync()
				.ConfigureAwait(false);

			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanGetPartialMiddle()
		{
			ITimeSeries timeSeries = GetTimeSeries();

			DateTimeOffset now = DateTimeOffset.UtcNow;
			await timeSeries.AddOrUpdate("A", "A", now, Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "B", now.AddDays(1), Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "C", now.AddDays(2), Array.Empty<byte>()).ConfigureAwait(false);

			ITimeSeriesEntry[]? all = await timeSeries
				.GetRange("A", now.AddTicks(1), now.AddDays(1).AddTicks(1))
				.ToArrayAsync()
				.ConfigureAwait(false);

			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanGetPartialUpper()
		{
			ITimeSeries timeSeries = GetTimeSeries();

			DateTimeOffset now = DateTimeOffset.UtcNow;
			await timeSeries.AddOrUpdate("A", "A", now, Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "B", now.AddDays(1), Array.Empty<byte>()).ConfigureAwait(false);
			await timeSeries.AddOrUpdate("A", "C", now.AddDays(2), Array.Empty<byte>()).ConfigureAwait(false);

			ITimeSeriesEntry[]? all = await timeSeries
				.GetRange("A", now.AddDays(1).AddTicks(1), DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);

			Assert.NotNull(all);
			Assert.Single(all);
		}
	}
}
