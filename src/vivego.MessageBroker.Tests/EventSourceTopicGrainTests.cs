using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Orleans.TestingHost;

using vivego.core;
using vivego.MessageBroker.EventStore;

using Xunit;
using Xunit.Abstractions;

namespace vivego.MessageBroker.Tests
{
	[Collection(nameof(TestSilo))]
	public sealed class EventSourceTopicGrainTests : AsyncDisposableBase
	{
		private readonly TestSilo _testSilo;

		public EventSourceTopicGrainTests(ITestOutputHelper testOutputHelper)
		{
			_testSilo = new TestSilo();
			_testSilo.TestOutputHelper = testOutputHelper;
			RegisterDisposable(_testSilo);
		}

		[Fact]
		public async Task CanRecoverGrainStateFromHardFailure()
		{
			IEventStore eventSource = _testSilo.SileServiceProvider.GetRequiredService<IEventStore>();
			string topic = Guid.NewGuid().ToString();
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);
			await eventSource.Append(topic, Array.Empty<byte>()).ConfigureAwait(false);

			// Kill silo.. EventSourceTopicGrain state is not stored and is lost
			await _testSilo.Cluster.KillSiloAsync(_testSilo.Cluster.Primary).ConfigureAwait(false);

			SiloHandle siloHandle = await _testSilo.Cluster.StartSiloAsync(0, _testSilo.Cluster.Options).ConfigureAwait(false);
			if (siloHandle is InProcessSiloHandle inProcessSiloHandle)
			{
				// Test grain can rebuild state from event history
				eventSource = inProcessSiloHandle.SiloHost.Services.GetRequiredService<IEventStore>();
				Abstractions.EventSourceEvent[] events = await eventSource
					.Get(topic, 0)
					.ToArrayAsync()
					.ConfigureAwait(false);
				Assert.Equal(3, events.Length);
				Assert.Equal(0, events[0].EventId);
				Assert.Equal(1, events[1].EventId);
				Assert.Equal(2, events[2].EventId);
			}
		}
	}
}
