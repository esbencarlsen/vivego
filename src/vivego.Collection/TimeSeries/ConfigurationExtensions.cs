using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.Index;
using vivego.Collection.Queue;
using vivego.KeyValue;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Collection.TimeSeries
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddTimeSeries(this IServiceCollection collection,
			Func<string, IIndexCompactionStrategy> compactionStrategyFactory,
			TimeSpan? cacheTimeout = default,
			string? name = default,
			string? keyValueStoreName = default,
			string? queueStoreName = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			TimeSeriesServiceBuilder serviceBuilder = new(name ?? "Default", collection, compactionStrategyFactory, cacheTimeout);
			serviceBuilder.DependsOn<IMemoryCache>();
			serviceBuilder.DependsOnNamedService<IKeyValueStore>(keyValueStoreName);
			serviceBuilder.DependsOnNamedService<IQueue>(queueStoreName);
			
			return serviceBuilder;
		}
	}
}