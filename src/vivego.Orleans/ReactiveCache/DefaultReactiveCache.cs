using System;
using System.Threading.Tasks;

using Orleans;

namespace vivego.Orleans.ReactiveCache;

public sealed class DefaultReactiveCache : IReactiveCache
{
	private readonly IClusterClient _clusterClient;

	public DefaultReactiveCache(IClusterClient clusterClient)
	{
		_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
	}

	public ValueTask Set<T>(string key, T value) where T : notnull
	{
		return _clusterClient
			.GetGrain<IProducerGrain<T>>(key)
			.Set(value);
	}

	public ValueTask<T?> Get<T>(string key) where T : notnull
	{
		return _clusterClient
			.GetGrain<IProducerCacheGrain<T>>(key)
			.Get();
	}
}
