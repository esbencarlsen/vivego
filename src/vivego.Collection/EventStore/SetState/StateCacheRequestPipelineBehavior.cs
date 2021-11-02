using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Caching.Memory;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.Collection.EventStore.SetState
{
	public sealed class StateCacheRequestPipelineBehavior :
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<GetRequest, KeyValueEntry>
	{
		private readonly IMemoryCache _memoryCache;
		private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions = new()
		{
			SlidingExpiration = TimeSpan.FromMinutes(1)
		};

		public StateCacheRequestPipelineBehavior(IMemoryCache memoryCache)
		{
			_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		}

		public async Task<string> Handle(SetRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));

			string etag = await next().ConfigureAwait(false);
			if (!string.IsNullOrEmpty(etag)
				&& request.Entry.MetaData.TryGetValue(nameof(SetStateRequest), out _)
				&& !request.Entry.Value.IsNull())
			{
				KeyValueEntry keyValueEntry = new()
				{
					Value = request.Entry.Value,
					ETag = etag,
					ExpiresAtUnixTimeSeconds = DateTimeOffset.UtcNow.AddSeconds(request.Entry.ExpiresInSeconds).ToUnixTimeSeconds()
				};
				keyValueEntry.MetaData.Add(request.Entry.MetaData);
				_memoryCache.Set(request.Entry.Key, keyValueEntry, _memoryCacheEntryOptions);
			}

			return etag;
		}

		public Task<KeyValueEntry> Handle(GetRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<KeyValueEntry> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));

			if (_memoryCache.TryGetValue(request.Key, out KeyValueEntry keyValueEntry))
			{
				return Task.FromResult(keyValueEntry);
			}

			return next();
		}
	}
}
