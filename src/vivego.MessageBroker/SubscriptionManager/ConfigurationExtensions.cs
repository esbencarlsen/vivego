using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace vivego.MessageBroker.SubscriptionManager;

public static class ConfigurationExtensions
{
	public static IServiceCollection TryAddSubscriptionManager(this IServiceCollection serviceCollection)
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

		if (serviceCollection.All(d => d.ServiceType != typeof(DefaultSubscriptionManager)))
		{
			serviceCollection.AddSingleton<DefaultSubscriptionManager>();
			serviceCollection.AddSingleton<ISubscriptionManager>(sp => sp.GetRequiredService<DefaultSubscriptionManager>());
			serviceCollection.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DefaultSubscriptionManager>());
		}

		return serviceCollection;
	}
}
