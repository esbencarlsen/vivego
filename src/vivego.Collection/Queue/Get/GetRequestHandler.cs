using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Collection.Queue.Get
{
	public sealed class GetRequestHandler : IRequestHandler<GetRequest, IQueueEntry?>
	{
		private readonly IKeyValueStore _keyValueStore;

		public GetRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<IQueueEntry?> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			string key = RequestHandlerHelper.MakeKey(request.Id, request.Version);
			KeyValueEntry keyValueEntry = await _keyValueStore
				.Get(key, cancellationToken)
				.ConfigureAwait(false);

			if (keyValueEntry is null || keyValueEntry.Value.IsNull())
			{
				return default;
			}

			return new QueueEntry(
				request.Version,
				request.Id,
				keyValueEntry.Value.ToBytes()!);
		}
	}
}
