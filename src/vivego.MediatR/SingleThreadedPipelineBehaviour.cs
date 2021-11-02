using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.core.Actors;

namespace vivego.MediatR
{
	internal sealed class SingleThreadedPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
	{
		private readonly ActorManager _actorManager;
		private readonly Func<TRequest, string> _keySelector;

		public SingleThreadedPipelineBehaviour(ActorManager actorManager,
			Func<TRequest, string> keySelector)
		{
			_actorManager = actorManager ?? throw new ArgumentNullException(nameof(actorManager));
			_keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
		}

		public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));
			string key = _keySelector(request);
			return _actorManager
				.GetActor(key)
				.Run(state => state(), next, cancellationToken: cancellationToken);
		}
	}
}