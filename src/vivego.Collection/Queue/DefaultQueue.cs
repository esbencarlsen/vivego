using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.Queue.Append;
using vivego.Collection.Queue.Get;
using vivego.Collection.Queue.GetAll;
using vivego.Collection.Queue.GetAllReverse;
using vivego.Collection.Queue.GetState;
using vivego.Collection.Queue.PeekFirst;
using vivego.Collection.Queue.PeekLast;
using vivego.Collection.Queue.Prepend;
using vivego.Collection.Queue.SetState;
using vivego.Collection.Queue.Truncate;
using vivego.Collection.Queue.TryTakeFirst;
using vivego.Collection.Queue.TryTakeLast;
using vivego.core;
using vivego.KeyValue;
using vivego.Queue.Model;

namespace vivego.Collection.Queue
{
	public sealed class DefaultQueue : IQueue, IQueueState
	{
		private readonly IMediator _mediator;
		private readonly IKeyValueStore _keyValueStore;

		public DefaultQueue(string name,
			IMediator mediator,
			IKeyValueStore keyValueStore)
		{
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public string Name { get; }

		public async ValueTask<long?> Append(string id, byte[] item, long? expectedVersion = default, TimeSpan? expiresIn = default, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new AppendRequest(id, item, expectedVersion, expiresIn), cancellationToken).ConfigureAwait(false);

		public async ValueTask<long?> Prepend(string id, byte[] item, long? expectedVersion = default, TimeSpan? expiresIn = default, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new PrependRequest(id, item, expectedVersion, expiresIn), cancellationToken).ConfigureAwait(false);

		public async ValueTask<IQueueEntry?> TryTakeLast(string id, bool fast = false, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new TryTakeLastRequest(id, fast), cancellationToken).ConfigureAwait(false);

		public async ValueTask<IQueueEntry?> TryTakeFirst(string id, bool fast = false, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new TryTakeFirstRequest(id, fast), cancellationToken).ConfigureAwait(false);

		public async ValueTask<IQueueEntry?> PeekLast(string id, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new PeekLastRequest(id), cancellationToken).ConfigureAwait(false);

		public async ValueTask<IQueueEntry?> PeekFirst(string id, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new PeekFirstRequest(id), cancellationToken).ConfigureAwait(false);

		public async ValueTask<long> Count(string id, CancellationToken cancellationToken = default)
		{
			QueueState state = await GetState(id, cancellationToken).ConfigureAwait(false);
			return state.Count();
		}

		public Task Truncate(string id, long? head = default, long? tail = default, bool fast = false, CancellationToken cancellationToken = default) =>
			_mediator.Send(new TruncateRequest(id, head, tail, fast), cancellationToken);

		public async ValueTask<IQueueEntry?> Get(string id, long index, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new GetRequest(id, index), cancellationToken).ConfigureAwait(false);

		public IAsyncEnumerable<IQueueEntry> GetAll(string id, long? skip = default, CancellationToken cancellationToken = default) =>
			_mediator
				.Send(new GetAllRequest(id, skip), cancellationToken)
				.Unwrap(cancellationToken);

		public IAsyncEnumerable<IQueueEntry> GetAllReverse(string id, long? skip = default, CancellationToken cancellationToken = default) =>
			_mediator
				.Send(new GetAllReverseRequest(id, skip), cancellationToken)
				.Unwrap(cancellationToken);

		public ValueTask<KeyValueStoreFeatures> GetFeatures() => _keyValueStore.GetFeatures();

		public async ValueTask<QueueState> GetState(string id, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new GetStateRequest(id), cancellationToken).ConfigureAwait(false);

		public async ValueTask SetState(string id, QueueState queueState, TimeSpan? expiresIn, CancellationToken cancellationToken = default) =>
			await _mediator.Send(new SetStateRequest(id, queueState, expiresIn), cancellationToken).ConfigureAwait(false);
	}
}
