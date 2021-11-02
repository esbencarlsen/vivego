using System;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Hosting;

using vivego.Collection.EventStore.Append;
using vivego.Collection.EventStore.Delete;
using vivego.Collection.EventStore.GetState;
using vivego.Collection.EventStore.SetState;
using vivego.Collection.Orleans.EventStore.Append;
using vivego.Collection.Orleans.EventStore.Delete;
using vivego.Collection.Orleans.EventStore.GetState;
using vivego.Collection.Orleans.EventStore.SetState;
using vivego.EventStore;
using vivego.ServiceBuilder.Abstractions;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.Orleans.EventStore
{
	public static class OrleansEventStoreConfigurationExtensions
	{
		public static IServiceBuilder AddOrleansSingleThreadAccessPipelineBehavior(this IServiceBuilder eventStoreBuilder)
		{
			if (eventStoreBuilder is null) throw new ArgumentNullException(nameof(eventStoreBuilder));

			eventStoreBuilder.DependsOn<IClusterClient>();
			eventStoreBuilder.Services.AddSingleton<IPipelineBehavior<AppendRequest, Version>, AppendRequestPipelineBehavior>();
			eventStoreBuilder.Services.AddSingleton<IPipelineBehavior<DeleteRequest, Unit>, DeleteRequestPipelineBehavior>();
			eventStoreBuilder.Services.AddSingleton<IPipelineBehavior<SetStateRequest, Unit>, SetStateRequestPipelineBehavior>();
			eventStoreBuilder.Services.AddSingleton<IPipelineBehavior<GetStateRequest, EventStoreState>, GetStateRequestPipelineBehavior>();

			return eventStoreBuilder;
		}

		public static ISiloBuilder ConfigureEventStoreApplicationParts(this ISiloBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));
			builder.ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(IEventStoreGrain).Assembly).WithReferences());
			return builder;
		}

		public static IClientBuilder ConfigureEventStoreApplicationParts(this IClientBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));
			builder.ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(IEventStoreGrain).Assembly).WithReferences());
			return builder;
		}
	}
}