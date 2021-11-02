using System;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.Queue;
using vivego.Collection.Queue.SetState;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.Collection
{
	public static class QueueConfigurationExtensions
	{
		public static IServiceBuilder AddQueue(
			this IServiceCollection collection,
			string? providerName = default,
			string? keyValueStoreName = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			QueueBuilder queueBuilder = new(providerName ?? "Default", collection);
			queueBuilder.DependsOnNamedService<IKeyValueStore>(keyValueStoreName);

			return queueBuilder;
		}
		
		public static IServiceBuilder AddStateCachePipelineBehavior(this IServiceBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.DependsOn<IMemoryCache>();
			builder.Services.AddSingleton<IPipelineBehavior<GetRequest, KeyValueEntry>, StateCacheRequestPipelineBehavior>();
			builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>, StateCacheRequestPipelineBehavior>();

			return builder;
		}
	}
}