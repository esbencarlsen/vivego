using System.Threading.Tasks;

using Orleans;

namespace vivego.Orleans.ReactiveCache;

public interface IProducerCacheGrain<T> : IGrainWithStringKey where T : notnull
{
	ValueTask<T?> Get();
}
