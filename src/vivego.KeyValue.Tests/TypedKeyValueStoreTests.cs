using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json;

using vivego.Serializer;

using Xunit;

namespace vivego.KeyValue.Tests
{
	public sealed class TypedKeyValueStoreTests : IAsyncLifetime
	{
		private readonly IHost _host;

		public TypedKeyValueStoreTests()
		{
			HostBuilder hostBuilder = new();
			hostBuilder.ConfigureServices(collection =>
			{
				collection.AddMemoryCache();
				collection.AddNewtonSoftJsonSerializer();
				collection.AddInMemoryKeyValueStore();
				collection.AddTypedKeyValueStore();
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

		public ITypedKeyValueStore MakeInMemoryKeyValueStore()
		{
			return _host.Services.GetRequiredService<ITypedKeyValueStore>();
		}

		[Fact]
		public async Task CanSetAndGetNewtonSoft()
		{
			ITypedKeyValueStore store = MakeInMemoryKeyValueStore();
			// ReSharper disable once MethodHasAsyncOverload
			object? entity = JsonConvert.DeserializeObject(@"{
    ""A"":1
}");

			await store
				.Set(new SetKeyValueEntry<object>("A", entity, -1, string.Empty))
				.ConfigureAwait(false);

			KeyValueEntry<object> result = await store
				.Get<object>("A")
				.ConfigureAwait(false);
			Assert.NotNull(result);
			Assert.NotNull(result.ETag);
			Assert.NotNull(result.Value);
		}
	}
}
