using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace vivego.KeyValue.Tests
{
	public sealed class CachedKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			serviceCollection
				.AddMemoryCache()
				.AddInMemoryKeyValueStore("UnitTest")
				.AddCachingKeyValueStoreBehaviour((__, _) => _.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1));
		}

		[Fact]
		public async Task DoNotCacheNullValues()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			byte[]? bytes = await keyValueStore.GetValue("A").ConfigureAwait(false);
			Assert.Null(bytes);

			IMemoryCache memoryCache = _host.Services.GetRequiredService<IMemoryCache>();
			object? o = memoryCache.Get("A");
			Assert.Null(o);
		}
	}
}
