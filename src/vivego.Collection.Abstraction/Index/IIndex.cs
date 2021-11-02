using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.Collection.Index
{
	public interface IIndex
	{
		Task Add(string key, Value field, Value? data = default, CancellationToken cancellationToken = default);
		Task Remove(string key, Value field, CancellationToken cancellationToken = default);
		Task<ImmutableSortedSet<IIndexEntry>> Get(string key, CancellationToken cancellationToken = default);
		Task Compact(string key, CancellationToken cancellationToken = default);
	}
}