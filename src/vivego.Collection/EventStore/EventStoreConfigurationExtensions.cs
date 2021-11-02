using System;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using vivego.Collection.EventStore.Append;
using vivego.Collection.EventStore.SetOptions;
using vivego.Collection.EventStore.SetState;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Set;
using vivego.MediatR;
using vivego.ServiceBuilder.Abstractions;

using DeleteRequest = vivego.Collection.EventStore.Delete.DeleteRequest;
using GetRequest = vivego.KeyValue.Get.GetRequest;
using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public static class EventStoreConfigurationExtensions
	{
		public static IServiceBuilder AddEventStore(this IServiceCollection collection,
			string? keyValueStoreName = default,
			string? eventStoreName = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			
			EventStoreBuilder eventStoreBuilder = new(eventStoreName ?? "Default", collection);
			eventStoreBuilder.DependsOnNamedService<IKeyValueStore>(keyValueStoreName);

			return eventStoreBuilder;
		}

		public static IServiceBuilder AddSingleThreadAccessPipelineBehavior(this IServiceBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.AddSingleThreadedPipelineBehaviour<AppendRequest, Version>(request => request.StreamId);
			builder.AddSingleThreadedPipelineBehaviour<DeleteRequest, Unit>(request => request.StreamId);
			builder.AddSingleThreadedPipelineBehaviour<SetOptionsRequest, Unit>(request => request.StreamId);

			return builder;
		}

		public static IServiceBuilder AddEventStoreStateCachePipelineBehavior(this IServiceBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.DependsOn<IMemoryCache>();
			builder.Services.TryAddSingleton<StateCacheRequestPipelineBehavior>();
			builder.Services.AddSingleton<IPipelineBehavior<GetRequest, KeyValueEntry>>(sp => sp.GetRequiredService<StateCacheRequestPipelineBehavior>());
			builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>, StateCacheRequestPipelineBehavior>(sp => sp.GetRequiredService<StateCacheRequestPipelineBehavior>());

			return builder;
		}
	}
}