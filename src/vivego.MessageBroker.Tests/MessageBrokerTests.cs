using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.MessageBroker.Abstractions;

using Xunit;
using Xunit.Abstractions;

namespace vivego.MessageBroker.Tests
{
	[Collection(nameof(TestSilo))]
	public sealed class MessageBrokerTests : IClassFixture<TestSilo>
	{
		private readonly TestSilo _testSilo;

		public MessageBrokerTests(TestSilo testSilo, ITestOutputHelper testOutputHelper)
		{
			_testSilo = testSilo ?? throw new ArgumentNullException(nameof(testSilo));
			_testSilo.TestOutputHelper = testOutputHelper;
		}

		[Fact]
		public async Task CanGetEmpty()
		{
			IMessageBroker messageBroker = _testSilo.SileServiceProvider.GetRequiredService<IMessageBroker>();
			string topic = Guid.NewGuid().ToString();
			Abstractions.MessageBrokerEvent[] events = await messageBroker
				.Get(topic, 0)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Empty(events);
		}

		[Fact]
		public async Task CanGetFromBehindWhenEmpty()
		{
			IMessageBroker messageBroker = _testSilo.SileServiceProvider.GetRequiredService<IMessageBroker>();
			string topic = Guid.NewGuid().ToString();
			Abstractions.MessageBrokerEvent[] events = await messageBroker
				.Get(topic, -1)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Empty(events);
		}

		[Fact]
		public async Task WillNotReturnElementsWithoutSubscription()
		{
			IMessageBroker messageBroker = _testSilo.SileServiceProvider.GetRequiredService<IMessageBroker>();
			string topic = Guid.NewGuid().ToString();
			await messageBroker.Publish(topic, Array.Empty<byte>()).ConfigureAwait(false);
			Abstractions.MessageBrokerEvent[] events = await messageBroker
				.Get(topic, -1)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.Empty(events);
		}
	}
}
