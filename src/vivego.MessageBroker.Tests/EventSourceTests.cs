using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.MessageBroker.EventStore;

using Xunit;
using Xunit.Abstractions;

namespace vivego.MessageBroker.Tests
{
	[Collection(nameof(TestSilo))]
	public sealed class EventSourceTests : IClassFixture<TestSilo>
	{
		private readonly TestSilo _testSilo;

		public EventSourceTests(TestSilo testSilo, ITestOutputHelper testOutputHelper)
		{
			_testSilo = testSilo ?? throw new ArgumentNullException(nameof(testSilo));
			_testSilo.TestOutputHelper = testOutputHelper;
		}

		[Fact]
		public async Task GetNextEventIdZeroWhenNew()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			long eventId = await eventSource.GetNextEventId(topic).ConfigureAwait(false);
			Assert.Equal(0, eventId);
		}

		[Fact]
		public async Task GetNextEventIdZeroWhenNotEmpty()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			long eventId = await eventSource.GetNextEventId(topic).ConfigureAwait(false);
			Assert.Equal(1, eventId);
		}

		[Fact]
		public async Task CanGetEmpty()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, 0)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Empty(events);
		}

		[Fact]
		public async Task CanGetFromBehindWhenEmpty()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, -1)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Empty(events);
		}

		[Fact]
		public async Task CanGetFromBehindWhenSingleElement()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, -1)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Single(events);
			Assert.Equal(0, events[0].EventId);
		}

		[Fact]
		public async Task CanGetMultipleFromBehindSingleElement()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, -2)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Single(events);
			Assert.Equal(0, events[0].EventId);
		}

		[Fact]
		public async Task CanGetMultipleFromBehindMultiElements()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, -2)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Equal(2, events.Length);
			Assert.Equal(0, events[0].EventId);
			Assert.Equal(1, events[1].EventId);
		}

		[Fact]
		public async Task CanGetSingleFromBehindMultiElements()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			await eventSource.Append(topic, new byte[1]).ConfigureAwait(false);
			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, -1)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Single(events);
			Assert.Single(events[0].Data);
		}

		[Fact]
		public async Task CanSeparateTopics()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic1 = Guid.NewGuid().ToString();
			string topic2 = Guid.NewGuid().ToString();
			await eventSource.Append(topic1, Array.Empty<byte>()).ConfigureAwait(false);
			await eventSource.Append(topic2, Array.Empty<byte>()).ConfigureAwait(false);
			Abstractions.EventSourceEvent[] events1 = await eventSource
				.Get(topic1, 0)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Single(events1);
			Assert.Empty(events1[0].Data);

			Abstractions.EventSourceEvent[] events2 = await eventSource
				.Get(topic1, 0)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Single(events2);
			Assert.Empty(events2[0].Data);
		}

		[Fact]
		public async Task ReturnsCorrectOrder()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			foreach (int _ in Enumerable.Range(0, 100))
			{
				await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			}

			Abstractions.EventSourceEvent[] events = await eventSource
				.Get(topic, 0)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Equal(100, events.Length);

			foreach (int i in Enumerable.Range(0, 100))
			{
				Assert.Equal(i, events[i].EventId);
			}
		}

		[Fact]
		public async Task ReturnsCorrectReverseOrder()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			foreach (int _ in Enumerable.Range(0, 100))
			{
				await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			}

			Abstractions.EventSourceEvent[] events = await eventSource
				.GetReverse(topic, 99)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Equal(100, events.Length);

			foreach (int i in Enumerable.Range(0, 100))
			{
				Assert.Equal(99 - i, events[i].EventId);
			}
		}
	}
}
