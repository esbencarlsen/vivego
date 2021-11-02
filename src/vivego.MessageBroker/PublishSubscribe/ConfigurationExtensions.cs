using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Hosting;

namespace vivego.MessageBroker.PublishSubscribe;

public static class ConfigurationExtensions
{
	public static IServiceCollection TryAddPublishSubscribe(this IServiceCollection serviceCollection)
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

		if (serviceCollection.All(d => d.ServiceType != typeof(DefaultPublishSubscribe)))
		{
			serviceCollection.AddSingleton<DefaultPublishSubscribe>();
			serviceCollection.AddSingleton<IPublishSubscribe>(sp => sp.GetRequiredService<DefaultPublishSubscribe>());
			serviceCollection.AddSingleton<INotificationGrainObserver>(sp => sp.GetRequiredService<DefaultPublishSubscribe>());
		}

		return serviceCollection;
	}

	public static ISiloBuilder PublishSubscribeConfigureApplicationParts(this ISiloBuilder siloBuilder)
	{
		if (siloBuilder is null) throw new ArgumentNullException(nameof(siloBuilder));

		siloBuilder.ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(IPubSubGrain).Assembly).WithReferences());

		return siloBuilder;
	}
}
