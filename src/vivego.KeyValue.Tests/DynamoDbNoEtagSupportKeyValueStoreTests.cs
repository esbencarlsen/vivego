using Amazon.DynamoDBv2;
using Amazon.Runtime;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.DynamoDb;

using Xunit;

namespace vivego.KeyValue.Tests;

[Trait("Category", "IntegrationTest")]
public sealed class DynamoDbNoEtagSupportKeyValueStoreTests : KeyValueStoreTests
{
	protected override void ConfigureServices(IServiceCollection serviceCollection)
	{
		serviceCollection
			.AddMemoryCache()
			.AddDynamoDbKeyValueStore("A",
				"deleteme",
				false,
				new BasicAWSCredentials("x", "x"),
				new AmazonDynamoDBConfig
				{
					ServiceURL = "http://localhost:8000"
				});
	}
}
