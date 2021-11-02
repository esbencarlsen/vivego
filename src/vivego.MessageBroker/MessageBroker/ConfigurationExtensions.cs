using System;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using vivego.KeyValue;
using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.EventStore;
using vivego.MessageBroker.SubscriptionManager;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.MessageBroker.MessageBroker;

public static class ConfigurationExtensions
{
	public static IServiceCollection TryAddMessageBroker(this IServiceCollection serviceCollection,
		IConfiguration configuration)
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (configuration is null) throw new ArgumentNullException(nameof(configuration));

		if (serviceCollection.All(d => d.ServiceType != typeof(DefaultMessageBroker)))
		{
			serviceCollection.AddSingleton(sp =>
			{
				IOptions<EventStoreOptions> options = sp.GetRequiredService<IOptions<EventStoreOptions>>();
				if (string.IsNullOrEmpty(options.Value.KeyValueStoreName))
				{
					return ActivatorUtilities.CreateInstance<DefaultMessageBroker>(sp);
				}

				IServiceManager<IKeyValueStore> serviceManager = sp.GetRequiredService<IServiceManager<IKeyValueStore>>();
				IKeyValueStore keyValueStore = serviceManager.Get(options.Value.KeyValueStoreName);
				return ActivatorUtilities.CreateInstance<DefaultMessageBroker>(sp, keyValueStore);
			});
			serviceCollection.AddSingleton<IMessageBroker>(sp => sp.GetRequiredService<DefaultMessageBroker>());

			serviceCollection.TryAddEventStore(configuration);
			serviceCollection.TryAddSubscriptionManager();
		}

		return serviceCollection;
	}
}
