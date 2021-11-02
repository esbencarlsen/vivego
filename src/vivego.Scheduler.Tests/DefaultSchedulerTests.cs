using System;
using System.Linq;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.Collection;
using vivego.Collection.Index;
using vivego.Collection.TimeSeries;
using vivego.core;
using vivego.KeyValue;
using vivego.Serializer;

using Xunit;

namespace vivego.Scheduler.Tests
{
	public sealed class DefaultSchedulerTests : DisposableBase
	{
		private static IScheduler GetSchduler()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			serviceCollection.AddInMemoryKeyValueStore();
			serviceCollection.AddQueue();
			serviceCollection.AddLogging();
			serviceCollection.AddTimeSeries(_ => new CounterIndexCompactionStrategy(1000));
			serviceCollection.AddSystemJsonSerializer();
			serviceCollection.AddScheduler();
			serviceCollection.AddMemoryCache();
			return serviceCollection.BuildServiceProvider().GetRequiredService<IScheduler>();
		}

		[Fact]
		public async Task CanGetAllWhenEmpty()
		{
			IScheduler scheduler = GetSchduler();
			IScheduledNotification[] all = await scheduler
				.GetAll(DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Empty(all);
		}

		[Fact]
		public async Task CanGetNonExistingEntry()
		{
			IScheduler scheduler = GetSchduler();
			IScheduledNotification? notification = await scheduler
				.Get(Guid.NewGuid().ToString())
				.ConfigureAwait(false);
			Assert.Null(notification);
		}

		[Fact]
		public async Task CanCancelNonExistingEntry()
		{
			IScheduler scheduler = GetSchduler();
			await scheduler.Cancel(Guid.NewGuid().ToString()).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanSchedule()
		{
			IScheduler scheduler = GetSchduler();
			await scheduler
				.Schedule("A", new TestNotification(), TimeSpan.FromSeconds(1))
				.ConfigureAwait(false);
		}

		[Fact]
		public async Task CanScheduleAndGet()
		{
			IScheduler scheduler = GetSchduler();
			string key = Guid.NewGuid().ToString();
			TestNotification testNotification = new();
			await scheduler
				.Schedule(key, testNotification, TimeSpan.FromSeconds(1))
				.ConfigureAwait(false);
			IScheduledNotification? notification = await scheduler
				.Get(key)
				.ConfigureAwait(false);
			Assert.NotNull(notification);
			Assert.Equal(key, notification?.Id);
			Assert.True(notification?.Notification is TestNotification);
			Assert.Equal(TimeSpan.FromSeconds(1), notification?.TriggerIn);
			Assert.Equal(testNotification.Value, ((TestNotification)notification?.Notification!).Value);
		}

		[Fact]
		public async Task CanScheduleAndGetAll()
		{
			IScheduler scheduler = GetSchduler();
			string key = Guid.NewGuid().ToString();
			await scheduler
				.Schedule(key, new TestNotification(), TimeSpan.FromSeconds(0))
				.ConfigureAwait(false);
			IScheduledNotification[] all = await scheduler
				.GetAll(DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanScheduleAndGetCancel()
		{
			IScheduler scheduler = GetSchduler();
			string key = Guid.NewGuid().ToString();
			await scheduler
				.Schedule(key, new TestNotification(), TimeSpan.FromSeconds(1))
				.ConfigureAwait(false);

			await scheduler
				.Cancel(key)
				.ConfigureAwait(false);

			IScheduledNotification[] all = await scheduler
				.GetAll(DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Empty(all);
		}
	}

	public sealed class TestNotification : INotification
	{
		public Guid Value { get; set; } = Guid.NewGuid();
	}
}
