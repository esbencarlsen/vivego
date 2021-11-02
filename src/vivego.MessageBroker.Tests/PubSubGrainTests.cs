using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.core;
using vivego.MessageBroker.PublishSubscribe;

using Xunit;
using Xunit.Abstractions;

namespace vivego.MessageBroker.Tests
{
	[Collection(nameof(TestSilo))]
	public sealed class PubSubGrainTests : AsyncDisposableBase
	{
		private readonly TestSilo _testSilo;

		public PubSubGrainTests(ITestOutputHelper testOutputHelper)
		{
			_testSilo = new TestSilo();
			_testSilo.TestOutputHelper = testOutputHelper;
			RegisterDisposable(_testSilo);
		}

		[Fact]
		public async Task CanRecoverGrainStateFromGrainReloadOrSiloMove()
		{
			string topic = Guid.NewGuid().ToString();
			IPublishSubscribe publishSubscribe = _testSilo.Cluster.Client.ServiceProvider.GetRequiredService<IPublishSubscribe>();

			using CancellationTokenSource cancellationTokenSource = new(10000);
			ValueTask<byte[][]> subscriptionTask = publishSubscribe
				.Subscribe(topic, cancellationTokenSource.Token)
				.Take(1)
				.ToArrayAsync(cancellationTokenSource.Token);
			// Wait for subscription to be registered.
			await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);

			// Restart silo.. PubSubGrain state will be persisted
			await _testSilo.Cluster.RestartSiloAsync(_testSilo.Cluster.Primary).ConfigureAwait(false);

			// Publish event
			await publishSubscribe.Publish(topic, Array.Empty<byte>()).ConfigureAwait(false);

			// Old subscription should have survived silo restart
			byte[][] all = await subscriptionTask.ConfigureAwait(false);
			Assert.Single(all);
		}
	}
}
