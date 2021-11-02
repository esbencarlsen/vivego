using System.Threading;
using System.Threading.Tasks;

using vivego.KeyValue.Abstractions.Model;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue;

public interface IKeyValueStore : INamedService
{
	ValueTask<KeyValueStoreFeatures> GetFeatures(CancellationToken cancellationToken = default);
	ValueTask<string> Set(SetKeyValueEntry setKeyValueEntry, CancellationToken cancellationToken = default);
	ValueTask<KeyValueEntry> Get(string key, CancellationToken cancellationToken = default);
	ValueTask<bool> Delete(DeleteKeyValueEntry deleteKeyValueEntry, CancellationToken cancellationToken = default);
	ValueTask Clear(CancellationToken cancellationToken = default);
}
