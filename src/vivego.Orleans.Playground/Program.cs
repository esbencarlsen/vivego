using Microsoft.Extensions.Hosting;

using Orleans;

using vivego.KeyValue;
using vivego.Orleans.KeyValueProvider;

IHostBuilder hostBuilder = new HostBuilder()
	.ConfigureOrleansDefaults(default, (_, siloBuilder) =>
	{
		siloBuilder.UseDashboard(dashboardOptions =>
		{
			dashboardOptions.HostSelf = true;
			dashboardOptions.Port = 20000;
		});
	})
	.ConfigureServices(collection =>
	{
		collection
			.AddInMemoryKeyValueStore("Default");
	});

await hostBuilder
	.RunConsoleAsync()
	.ConfigureAwait(false);
