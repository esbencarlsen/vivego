using System.Threading;
using System.Threading.Tasks;

using vivego.KeyValue.Abstractions.Model;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue;

public interface ITypedKeyValueStore : INamedService
{
	ValueTask<KeyValueStoreFeatures> GetFeatures(CancellationToken cancellationToken = default);
	ValueTask<string> Set<T>(SetKeyValueEntry<T> setKeyValueEntry, CancellationToken cancellationToken = default) where T : notnull;
	ValueTask<KeyValueEntry<T>> Get<T>(string key, CancellationToken cancellationToken = default) where T : notnull;
	ValueTask<bool> Delete(DeleteKeyValueEntry deleteKeyValueEntry, CancellationToken cancellationToken = default);
}
