using System.Threading.Tasks;

namespace vivego.Orleans.ReactiveCache;

public interface IReactiveCache
{
	ValueTask Set<T>(string key, T value) where T : notnull;
	ValueTask<T?> Get<T>(string key) where T : notnull;
}
