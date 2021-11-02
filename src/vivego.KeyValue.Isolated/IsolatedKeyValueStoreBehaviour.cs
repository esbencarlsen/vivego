using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;

using vivego.core.Actors;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Isolated
{
	public sealed class IsolatedKeyValueStoreBehaviour :
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<GetRequest, KeyValueEntry>,
		IPipelineBehavior<DeleteRequest, bool>
	{
		private readonly ActorManager _actorManager;

		public IsolatedKeyValueStoreBehaviour(ILogger<IsolatedKeyValueStoreBehaviour> logger)
		{
			if (logger is null) throw new ArgumentNullException(nameof(logger));

			_actorManager = new ActorManager(logger);
		}

		public Task<string> Handle(SetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			return _actorManager
				.GetActor(request.Entry.Key)
				.Run(state => state(), next, cancellationToken: cancellationToken);
		}

		public Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<KeyValueEntry> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
			return _actorManager
				.GetActor(request.Key)
				.Run(state => state(), next, cancellationToken: cancellationToken);
		}

		public Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			return _actorManager
				.GetActor(request.Entry.Key)
				.Run(state => state(), next, cancellationToken: cancellationToken);
		}
	}
}
