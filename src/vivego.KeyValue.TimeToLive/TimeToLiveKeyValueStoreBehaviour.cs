using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;
using vivego.Scheduler;

namespace vivego.KeyValue.TimeToLive
{
	public sealed class TimeToLiveKeyValueStoreBehaviour :
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<GetRequest, KeyValueEntry>,
		INotificationHandler<TimeToLiveNotification>
	{
		private readonly IScheduler _scheduler;
		private readonly IKeyValueStore _keyValueStore;

		public TimeToLiveKeyValueStoreBehaviour(
			IScheduler scheduler,
			IKeyValueStore keyValueStore)
		{
			_scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			string etag = await next().ConfigureAwait(false);
			if (request.Entry.ExpiresInSeconds > 0 && !string.IsNullOrEmpty(etag))
			{
				await _scheduler
					.Schedule(request.Entry.Key + "_TTL", new TimeToLiveNotification
					{
						Key = request.Entry.Key,
						ETag = etag
					}, TimeSpan.FromSeconds(request.Entry.ExpiresInSeconds), null, cancellationToken)
					.ConfigureAwait(false);
			}

			return etag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<KeyValueEntry> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			KeyValueEntry result = await next().ConfigureAwait(false);
			if (result.ExpiresAtUnixTimeSeconds > 0
				&& DateTimeOffset.FromUnixTimeSeconds(result.ExpiresAtUnixTimeSeconds) < DateTimeOffset.UtcNow)
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}

			return result;
		}

		public async Task Handle(TimeToLiveNotification notification, CancellationToken cancellationToken)
		{
			if (notification is null) throw new ArgumentNullException(nameof(notification));
			await _keyValueStore
				.DeleteEntry(notification.Key, notification.ETag, cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
