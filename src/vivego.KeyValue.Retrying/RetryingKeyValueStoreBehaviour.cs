using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Retrying
{
	public sealed class RetryingKeyValueStoreBehaviour :
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<GetRequest, KeyValueEntry>,
		IPipelineBehavior<DeleteRequest, bool>,
		IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>
	{
		private readonly int _maxRetries;
		private readonly Func<int, Exception, bool> _retryingPredicate;

		public RetryingKeyValueStoreBehaviour(
			int maxRetries,
			Func<int, Exception, bool> retryingPredicate)
		{
			if (maxRetries <= 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
			_maxRetries = maxRetries;
			_retryingPredicate = retryingPredicate ?? throw new ArgumentNullException(nameof(retryingPredicate));
		}

		private async Task<TResult> Retry<TResult>(RequestHandlerDelegate<TResult> next,
			CancellationToken cancellationToken)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			List<Exception>? exceptions = default;
			foreach (int i in Enumerable.Range(1, _maxRetries))
			{
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					return await next().ConfigureAwait(false);
				}
				catch (Exception e)
				{
					exceptions ??= new List<Exception>();
					exceptions.Add(e);
					if (!_retryingPredicate(i, e))
					{
						break;
					}
				}
			}

			if (exceptions is null)
			{
				throw new AggregateException($"Error while retrying, max retries: {_maxRetries}");
			}

			throw new AggregateException($"Error while retrying, max retries: {_maxRetries}", exceptions);
		}

		public Task<string> Handle(SetRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			return Retry(next, cancellationToken);
		}

		public Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<KeyValueEntry> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			return Retry(next, cancellationToken);
		}

		public Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			return Retry(next, cancellationToken);
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<KeyValueStoreFeatures> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			return Retry(next, cancellationToken);
		}
	}
}
