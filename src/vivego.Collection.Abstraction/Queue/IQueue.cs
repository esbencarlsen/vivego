#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using vivego.KeyValue;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Collection.Queue
{
	public interface IQueue : INamedService
	{
		ValueTask<long?> Append(string id, byte[] item, long? expectedVersion = default, TimeSpan? expiresIn = default, CancellationToken cancellationToken = default);
		ValueTask<long?> Prepend(string id, byte[] item, long? expectedVersion = default, TimeSpan? expiresIn = default, CancellationToken cancellationToken = default);

		ValueTask<IQueueEntry?> TryTakeLast(string id, bool fast = false, CancellationToken cancellationToken = default);
		ValueTask<IQueueEntry?> TryTakeFirst(string id, bool fast = false, CancellationToken cancellationToken = default);

		ValueTask<IQueueEntry?> PeekLast(string id, CancellationToken cancellationToken = default);
		ValueTask<IQueueEntry?> PeekFirst(string id, CancellationToken cancellationToken = default);

		ValueTask<long> Count(string id, CancellationToken cancellationToken = default);
		Task Truncate(string id, long? head = default, long? tail = default, bool fast = false, CancellationToken cancellationToken = default);

		ValueTask<IQueueEntry?> Get(string id, long index, CancellationToken cancellationToken = default);
		IAsyncEnumerable<IQueueEntry> GetAll(string id, long? skip = default, CancellationToken cancellationToken = default);
		IAsyncEnumerable<IQueueEntry> GetAllReverse(string id, long? skip = default, CancellationToken cancellationToken = default);

		ValueTask<KeyValueStoreFeatures> GetFeatures();
	}
}
