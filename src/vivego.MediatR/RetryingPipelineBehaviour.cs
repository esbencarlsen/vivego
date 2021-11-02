using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Polly;

namespace vivego.MediatR
{
	internal sealed class RetryingPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
	{
		private readonly IAsyncPolicy<TResponse> _policy;

		public RetryingPipelineBehaviour(IAsyncPolicy<TResponse> policy)
		{
			_policy = policy ?? throw new ArgumentNullException(nameof(policy));
		}

		public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
			return _policy.ExecuteAsync(_ => next(), cancellationToken);
		}
	}
}