using Cassandra;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace vivego.KeyValue.Tests
{
	[Trait("Category", "IntegrationTest")]
	public sealed class CassandraNoEtagKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddCassandraKeyValueStore("UnitTest",
					"Contact Points=localhost;Port=9042;Default Keyspace=deleteme",
					true,
					"deleteme",
					ConsistencyLevel.LocalOne,
					false);
		}
	}

	[Trait("Category", "IntegrationTest")]
	public sealed class CassandraEtagKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddCassandraKeyValueStore("UnitTest",
					"Contact Points=localhost;Port=9042;Default Keyspace=deleteme",
					true,
					"deleteme",
					ConsistencyLevel.LocalOne,
					true);
		}
	}
}
