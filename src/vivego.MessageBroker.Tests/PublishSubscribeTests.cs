using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.MessageBroker.PublishSubscribe;

using Xunit;
using Xunit.Abstractions;

namespace vivego.MessageBroker.Tests
{
	[Collection(nameof(TestSilo))]
	public sealed class PublishSubscribeTests : IClassFixture<TestSilo>
	{
		private readonly TestSilo _testSilo;

		public PublishSubscribeTests(TestSilo testSilo, ITestOutputHelper testOutputHelper)
		{
			_testSilo = testSilo ?? throw new ArgumentNullException(nameof(testSilo));
			_testSilo.TestOutputHelper = testOutputHelper;
		}

		[Fact]
		public async Task CanPublish()
		{
			IPublishSubscribe publishSubscribe = _testSilo.SileServiceProvider.GetRequiredService<IPublishSubscribe>();
			string topic = Guid.NewGuid().ToString();
			await publishSubscribe.Publish(topic, Array.Empty<byte>()).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanSubscribeAndCancel()
		{
			IPublishSubscribe publishSubscribe = _testSilo.SileServiceProvider.GetRequiredService<IPublishSubscribe>();
			string topic = Guid.NewGuid().ToString();
			using CancellationTokenSource cancellationTokenSource = new(100);
			byte[][] all = await publishSubscribe
				.Subscribe(topic, cancellationTokenSource.Token)
				.ToArrayAsync(cancellationTokenSource.Token)
				.ConfigureAwait(false);
			Assert.Empty(all);
		}

		[Fact]
		public async Task CanSubscribeAndPublish()
		{
			IPublishSubscribe publishSubscribe = _testSilo.SileServiceProvider.GetRequiredService<IPublishSubscribe>();
			string topic = Guid.NewGuid().ToString();
			using CancellationTokenSource cancellationTokenSource = new(1000);
			ValueTask<byte[][]> subscriptionTask = publishSubscribe
				.Subscribe(topic, cancellationTokenSource.Token)
				.Take(1)
				.ToArrayAsync(cancellationTokenSource.Token);

			// Wait for subscription to be registered.
			await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);

			await publishSubscribe.Publish(topic, Array.Empty<byte>()).ConfigureAwait(false);

			byte[][] all = await subscriptionTask.ConfigureAwait(false);
			Assert.Single(all);
		}
	}
}
