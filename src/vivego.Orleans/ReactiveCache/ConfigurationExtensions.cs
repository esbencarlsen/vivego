using System;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Hosting;

namespace vivego.Orleans.ReactiveCache;

public static class ConfigurationExtensions
{
	public static ISiloBuilder AddReactiveCache(this ISiloBuilder siloBuilder)
	{
		return siloBuilder
			.ConfigureServices(services =>
			{
				services.AddReactiveCache();
			})
			.ConfigureApplicationParts(manager =>
				manager.AddApplicationPart(typeof(ProducerGrain<>).Assembly).WithReferences());
	}

	public static IServiceCollection AddReactiveCache(this IServiceCollection services)
	{
		if (services is null) throw new ArgumentNullException(nameof(services));
		return services.AddSingleton<IReactiveCache, DefaultReactiveCache>();
	}

	public static IClientBuilder AddReactiveCache(this IClientBuilder clientBuilder)
	{
		if (clientBuilder is null) throw new ArgumentNullException(nameof(clientBuilder));
		return clientBuilder
			.ConfigureServices(services =>
			{
				services.AddReactiveCache();
			})
			.ConfigureApplicationParts(manager =>
				manager.AddApplicationPart(typeof(ProducerGrain<>).Assembly).WithReferences());
	}
}
