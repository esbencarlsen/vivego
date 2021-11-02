using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace vivego.KeyValue.Tests
{
	[Trait("Category", "IntegrationTest")]
	public sealed class S3KeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddS3KeyValueStore("UnitTest", new AmazonS3Options
				{
					BucketName = "unittest",
					AccessKey = "minioadmin",
					SecretKey = "minioadmin",
					Endpoint = "http://localhost:9000",
					RegionEndpoint = "USEast1"
				});
		}
	}
}
