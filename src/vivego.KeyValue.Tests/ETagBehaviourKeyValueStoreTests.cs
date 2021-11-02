using Microsoft.Extensions.DependencyInjection;

namespace vivego.KeyValue.Tests
{
	public sealed class ETagBehaviourKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddInMemoryKeyValueStore("UnitTest")
				.AddETagKeyValueBehaviour();
		}
	}
}
