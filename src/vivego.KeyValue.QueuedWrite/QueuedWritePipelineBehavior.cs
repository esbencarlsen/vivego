using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using MediatR;

using Microsoft.Extensions.Logging;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.QueuedWrite
{
	public sealed class QueuedWritePipelineBehavior :
		IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>,
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<DeleteRequest, bool>,
		IPipelineBehavior<GetRequest, KeyValueEntry>
	{
		private readonly ActionBlock<Func<Task>> _actionBlock;
		private readonly Task<bool> _trueTask = Task.FromResult(true);

		public QueuedWritePipelineBehavior(
			int maxDegreeOfParallelism,
			ILogger<QueuedWritePipelineBehavior> logger)
		{
			_actionBlock = new(async func =>
			{
				try
				{
					await func().ConfigureAwait(false);
				}
				catch (Exception e)
				{
					logger.LogError(e, "Error in QueuedWritePipelineBehavior execution loop");
				}
			}, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreeOfParallelism,
				SingleProducerConstrained = false
			});
		}

		public async Task<KeyValueStoreFeatures> Handle(
			FeaturesRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<KeyValueStoreFeatures> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));

			KeyValueStoreFeatures result = await next().ConfigureAwait(false);
			result.SupportsEtag = false;
			return result;
		}

		public Task<string> Handle(SetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));

			_actionBlock.Post(() => next());

			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry();
			return Task.FromResult(keyValueEntry.ETag);
		}

		public Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));

			_actionBlock.Post(() => next());

			return _trueTask;
		}

		public Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<KeyValueEntry> next)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

			TaskCompletionSource<KeyValueEntry> taskCompletionSource = new();
			_actionBlock.Post(async () =>
			{
				try
				{
					KeyValueEntry keyValueEntry = await next().ConfigureAwait(false);
					taskCompletionSource.SetResult(keyValueEntry);
				}
				catch (Exception e)
				{
					taskCompletionSource.SetException(e);
				}
			});
			return taskCompletionSource.Task;
		}
	}
}
