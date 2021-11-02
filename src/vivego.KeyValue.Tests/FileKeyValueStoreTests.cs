using System.IO;

using Microsoft.Extensions.DependencyInjection;

namespace vivego.KeyValue.Tests
{
	public sealed class FileKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddFileKeyValueStore("UnitTest", Path.Combine(Path.GetTempPath(), "deleteme"));
		}
	}
}
