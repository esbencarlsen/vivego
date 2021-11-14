using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.KeyValue.AzureTableStorage;
using vivego.Serializer;

using Xunit;

namespace vivego.KeyValue.Tests
{
	public abstract class KeyValueStoreTests : IAsyncLifetime
	{
#pragma warning disable CA1051
		// ReSharper disable InconsistentNaming
		protected readonly IHost _host;
		protected readonly IConfigurationRoot? _configuration;

		protected KeyValueStoreTests()
		{
			ConfigurationBuilder configurationBuilder = new();
			configurationBuilder.AddUserSecrets<KeyValueStoreTests>(true);
			_configuration = configurationBuilder.Build();
			HostBuilder hostBuilder = new();
			hostBuilder.ConfigureServices(collection =>
			{
				collection.AddMemoryCache();
				collection.AddLogging();
				collection.AddNewtonSoftJsonSerializer();
				ConfigureServices(collection);
			});
			_host = hostBuilder.Build();
		}

		public async Task InitializeAsync()
		{
			await _host.StartAsync().ConfigureAwait(false);
		}

		public async Task DisposeAsync()
		{
			await _host.StopAsync().ConfigureAwait(false);
			_host.Dispose();
		}

		protected abstract void ConfigureServices(IServiceCollection serviceCollection);

		[Fact]
		public async Task CanSetNull()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await keyValueStore.Set("A", default!).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanSetEmpty()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await keyValueStore.Set(Guid.NewGuid().ToString(), Array.Empty<byte>()).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotSetNullKey()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await Assert.ThrowsAsync<ArgumentNullException>(async () => await keyValueStore.Set(default!, Array.Empty<byte>()).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotSetEmptyKey()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await Assert.ThrowsAsync<ArgumentException>(async () => await keyValueStore.Set(string.Empty, Array.Empty<byte>()).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanGetEmpty()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			byte[]? value = await keyValueStore.GetValue(Guid.NewGuid().ToString()).ConfigureAwait(false);
			Assert.Null(value);
		}

		[Fact]
		public async Task CannotGetNullKey()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await Assert.ThrowsAsync<ArgumentException>(async () => await keyValueStore.Get(default!).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotGetEmptyKey()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await Assert.ThrowsAsync<ArgumentException>(async () => await keyValueStore.Get(string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotDeleteNullKey()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await Assert.ThrowsAsync<ArgumentNullException>(async () => await keyValueStore.DeleteEntry(default!).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotDeleteEmptyKey()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			await Assert.ThrowsAsync<ArgumentException>(async () => await keyValueStore.DeleteEntry(string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Fact]
		public async Task CanSetAndGet()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();
			string key = Guid.NewGuid().ToString();
			await keyValueStore.Set(key, Array.Empty<byte>()).ConfigureAwait(false);
			byte[]? value = await keyValueStore.GetValue(key).ConfigureAwait(false);
			Assert.NotNull(value);
			Assert.Empty(value!);
		}

		[Fact]
		public async Task CanSetAndDeleteAndGet()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			string key = Guid.NewGuid().ToString();
			await keyValueStore.Set(key, Array.Empty<byte>()).ConfigureAwait(false);
			byte[]? valueBeforeDelete = await keyValueStore.GetValue(key).ConfigureAwait(false);
			bool deleteResult = await keyValueStore.DeleteEntry(key).ConfigureAwait(false);
			byte[]? valueAfterDelete = await keyValueStore.GetValue(key).ConfigureAwait(false);

			Assert.NotNull(valueBeforeDelete);
			Assert.Empty(valueBeforeDelete!);
			Assert.True(deleteResult);
			Assert.Null(valueAfterDelete);
		}

		[Fact]
		public async Task CanSetAndSetFailWithETag()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			string key = Guid.NewGuid().ToString();
			string etag = await keyValueStore
				.Set(key, Array.Empty<byte>(), string.Empty)
				.ConfigureAwait(false);

			Assert.NotNull(etag);

			KeyValueStoreFeatures features = await keyValueStore.GetFeatures().ConfigureAwait(false);
			if (features.SupportsEtag)
			{
				string etag2 = await keyValueStore
					.Set(key, Array.Empty<byte>(), "NonValidEtag")
					.ConfigureAwait(false);
				Assert.Empty(etag2);
			}
		}

		[Fact]
		public virtual async Task CanSetAndSetWithETag()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			string key = Guid.NewGuid().ToString();
			string etag = await keyValueStore
				.Set(key, Array.Empty<byte>(), string.Empty)
				.ConfigureAwait(false);

			Assert.NotNull(etag);

			KeyValueStoreFeatures features = await keyValueStore.GetFeatures().ConfigureAwait(false);
			if (features.SupportsEtag)
			{
				string etag2 = await keyValueStore
					.Set(key, Array.Empty<byte>(), etag)
					.ConfigureAwait(false);
				Assert.NotNull(etag2);
				Assert.Equal(etag, etag2);
			}
		}

		[Fact]
		public async Task CanSetAndDeleteFailWithETag()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			string key = Guid.NewGuid().ToString();
			string etag = await keyValueStore
				.Set(key, Array.Empty<byte>(), string.Empty)
				.ConfigureAwait(false);

			Assert.NotNull(etag);

			KeyValueStoreFeatures features = await keyValueStore.GetFeatures().ConfigureAwait(false);
			if (features.SupportsEtag)
			{
				bool deleted = await keyValueStore
					.DeleteEntry(key, "NonValidEtag")
					.ConfigureAwait(false);
				Assert.False(deleted);
			}
		}

		[Fact]
		public async Task CanSetAndDeleteWithETag()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			string key = Guid.NewGuid().ToString();
			string etag = await keyValueStore
				.Set(key, Array.Empty<byte>(), string.Empty)
				.ConfigureAwait(false);

			Assert.NotNull(etag);

			KeyValueStoreFeatures features = await keyValueStore.GetFeatures().ConfigureAwait(false);
			if (features.SupportsEtag)
			{
				bool deleted = await keyValueStore
					.DeleteEntry(key, etag)
					.ConfigureAwait(false);
				Assert.True(deleted);
			}
		}

		[Fact]
		public async Task CanSetWithTtl()
		{
			IKeyValueStore keyValueStore = _host.Services.GetRequiredService<IKeyValueStore>();

			KeyValueStoreFeatures features = await keyValueStore.GetFeatures().ConfigureAwait(false);
			if (!features.SupportsTtl)
			{
				return;
			}

			string key = Guid.NewGuid().ToString();
			await keyValueStore
				.Set(key, Array.Empty<byte>(), string.Empty, TimeSpan.FromSeconds(1))
				.ConfigureAwait(false);

			await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

			byte[]? value = await keyValueStore.GetValue(key).ConfigureAwait(false);
			Assert.Null(value);
		}
	}
}
