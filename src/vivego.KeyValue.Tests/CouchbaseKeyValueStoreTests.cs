using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Couchbase;

using Xunit;

namespace vivego.KeyValue.Tests;

[Trait("Category", "IntegrationTest")]
public sealed class CouchbaseValueStoreTests : KeyValueStoreTests
{
	protected override void ConfigureServices(IServiceCollection serviceCollection)
	{
		serviceCollection
			.AddCouchbaseKeyValueStore("A",
				"couchbase://localhost",
				"deleteme",
				"deleteme",
				"deleteme");
	}

	[Fact(Skip = "Not supported in Couchbase")]
	public override Task CanSetAndSetWithETag()
	{
		throw new NotImplementedException();
	}
}
