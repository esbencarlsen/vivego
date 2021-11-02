using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.Collection.EventStore;
using vivego.KeyValue;
using vivego.Serializer;

namespace vivego.Collection.Tests.EventStore
{
	public sealed class DefaultEventStoreTests : EventStoreTests
	{
		protected override IEventStore MakeEventStore()
		{
			IHost host = new HostBuilder()
				.ConfigureServices(collection =>
				{
					collection.AddInMemoryKeyValueStore("Default");
					collection.AddEventStore();
					collection.AddLogging();
					collection.AddSystemJsonSerializer();
					collection.AddMemoryCache();
				})
				.Build();
			host.Start();
			RegisterDisposable(async () =>
			{
				using (host)
				{
					await host.StopAsync().ConfigureAwait(false);
				}
			});

			return host.Services.GetRequiredService<IEventStore>();
		}
	}
}
