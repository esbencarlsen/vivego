using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace vivego.KeyValue.Tests;

[Trait("Category", "IntegrationTest")]
public sealed class AzureTableStorageKeyValueStoreTests : KeyValueStoreTests
{
	protected override void ConfigureServices(IServiceCollection serviceCollection)
	{
		serviceCollection
			.AddMemoryCache()
			.AddAzureTableStorageKeyValueStore(
				"A",
				_configuration.GetValue<string>("AzureTableStorageConnectionString"),
				"UnitTestDeleteMe");
	}

	[Fact(Skip = "Not supported in Azure Table Storage")]
	public override Task CanSetAndSetWithETag()
	{
		throw new NotImplementedException();
	}
}
