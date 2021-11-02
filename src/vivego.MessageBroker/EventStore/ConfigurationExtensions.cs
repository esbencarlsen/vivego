using System;
using System.Linq;

using MediatR;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Hosting;

using vivego.KeyValue;
using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.EventStore.Append;
using vivego.MessageBroker.EventStore.GetEvent;
using vivego.MessageBroker.EventStore.GetNextEventId;
using vivego.MessageBroker.PublishSubscribe;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.MessageBroker.EventStore;

public static class ConfigurationExtensions
{
	public static IServiceCollection TryAddEventStore(this IServiceCollection serviceCollection,
		IConfiguration configuration)
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (configuration is null) throw new ArgumentNullException(nameof(configuration));

		if (serviceCollection.All(d => d.ServiceType != typeof(DefaultEventStore)))
		{
			serviceCollection.TryAddPublishSubscribe();

			serviceCollection.AddSingleton<IEventStore, DefaultEventStore>();
			serviceCollection
				.AddOptions<EventStoreOptions>()
				.Bind(configuration)
				.ValidateDataAnnotations();

			serviceCollection.AddSingleton<IRequestHandler<AppendRequest, long>, AppendRequestHandler>();

			serviceCollection.AddSingleton<PublishingPipelineBehaviour>();
			serviceCollection.AddSingleton<IPipelineBehavior<AppendRequest, long>>(sp => sp.GetRequiredService<PublishingPipelineBehaviour>());
			serviceCollection.AddSingleton<IHostedService>(sp => sp.GetRequiredService<PublishingPipelineBehaviour>());

			serviceCollection.AddSingleton<IRequestHandler<GetEventSourceEvent, EventSourceEvent?>, GetEventSourceEventHandler>(sp =>
			{
				IOptions<EventStoreOptions> options = sp.GetRequiredService<IOptions<EventStoreOptions>>();
				IServiceManager<IKeyValueStore> serviceManager = sp.GetRequiredService<IServiceManager<IKeyValueStore>>();
				IKeyValueStore keyValueStore = string.IsNullOrEmpty(options.Value.KeyValueStoreName)
					? serviceManager.GetAll().First()
					: serviceManager.Get(options.Value.KeyValueStoreName);
				return ActivatorUtilities.CreateInstance<GetEventSourceEventHandler>(sp, keyValueStore);
			});
			serviceCollection.AddSingleton<IRequestHandler<GetNextEventIdRequest, long>, GetNextEventIdRequestHandler>();
		}

		return serviceCollection;
	}

	public static ISiloBuilder EventStoreConfigureApplicationParts(this ISiloBuilder siloBuilder)
	{
		if (siloBuilder is null) throw new ArgumentNullException(nameof(siloBuilder));

		siloBuilder.ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(IEventStoreTopicGrain).Assembly).WithReferences());

		return siloBuilder;
	}
}
