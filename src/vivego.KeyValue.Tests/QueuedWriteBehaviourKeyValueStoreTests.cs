using Microsoft.Extensions.DependencyInjection;

namespace vivego.KeyValue.Tests
{
	public sealed class QueuedWriteBehaviourKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddInMemoryKeyValueStore("UnitTest")
				.AddQueuedWritePipelineBehavior(1, true);
		}
	}
}
