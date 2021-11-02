using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Tests.Helpers;

namespace vivego.KeyValue.Tests
{
	public sealed class GrpcServerKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddSingleton<IKeyValueStore, GrpcServerKeyValueStoreUnitTest>();
		}
	}
}
