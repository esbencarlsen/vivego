using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace vivego.KeyValue.Tests
{
	[Trait("Category", "IntegrationTest")]
	public sealed class RedisKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddRedisKeyValueStore("UnitTest", "localhost:6379", "UnitTest", false);
		}
	}
}
