using System.Threading.Tasks;

using Orleans;

namespace vivego.Orleans.ReactiveCache;

public interface IProducerGrain<T> : IGrainWithStringKey
{
	ValueTask Set(T value);
	ValueTask<VersionedValue<T>> Get();
	ValueTask<VersionedValue<T>> LongPoll(VersionToken knownVersion);
}
