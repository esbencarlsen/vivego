using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Caching.Memory;

using vivego.core;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

#pragma warning disable CA2208
namespace vivego.KeyValue.InMemory
{
	public sealed class InMemoryKeyValueStoreRequestHandler : DisposableBase, IKeyValueStoreRequestHandler
	{
		private readonly ScopedCache _scopedCache;
		private Task<KeyValueStoreFeatures>? _features;

		public InMemoryKeyValueStoreRequestHandler(IMemoryCache memoryCache)
		{
			_scopedCache = new ScopedCache("InMemoryKeyValueStoreRequestHandler_" + Guid.NewGuid(),
				TimeSpan.FromDays(31),
				memoryCache);
			RegisterDisposable(_scopedCache);
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
		{
			return _features ??= Task.FromResult(new KeyValueStoreFeatures
			{
				MaximumDataSize = int.MaxValue,
				MaximumKeyLength = int.MaxValue,
				SupportsTtl = false,
				SupportsEtag = true
			});
		}

		public Task<string> Handle(SetRequest setRequest, CancellationToken cancellationToken)
		{
			if (setRequest.Entry is null) throw new ArgumentNullException(nameof(setRequest.Entry));
			if (string.IsNullOrEmpty(setRequest.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(setRequest.Entry.Key));

			string scopeKey = MakeKey(setRequest.Entry.Key);
			KeyValueEntry keyValueEntry = setRequest.Entry.ConvertToKeyValueEntry();
			if (!string.IsNullOrEmpty(setRequest.Entry.ETag)
				&& _scopedCache.TryGetValue(scopeKey, out KeyValueEntry? value)
				&& value is not null
				&& !setRequest.Entry.ETag.Equals(value.ETag, StringComparison.Ordinal))
			{
				return Task.FromResult(string.Empty);
			}

			if (setRequest.Entry.ExpiresInSeconds > 0)
			{
				_scopedCache.Set(scopeKey, keyValueEntry, TimeSpan.FromSeconds(setRequest.Entry.ExpiresInSeconds));
			}
			else
			{
				_scopedCache.Set(scopeKey, keyValueEntry);
			}

			return Task.FromResult(keyValueEntry.ETag);
		}

		public Task<KeyValueEntry> Handle(GetRequest getRequest, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(getRequest.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(getRequest.Key));

			string scopeKey = MakeKey(getRequest.Key);
			if (_scopedCache.TryGetValue(scopeKey, out KeyValueEntry? keyValueEntry)
				&& keyValueEntry is not null)
			{
				return Task.FromResult(keyValueEntry);
			}

			return Task.FromResult(KeyValueEntryExtensions.KeyValueNull);
		}

		public Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			string scopeKey = MakeKey(request.Entry.Key);
			if (string.IsNullOrEmpty(request.Entry.ETag))
			{
				if (_scopedCache.TryGetValue(scopeKey, out _))
				{
					_scopedCache.Remove(scopeKey);
					return Task.FromResult(true);
				}

				return Task.FromResult(false);
			}

			if (_scopedCache.TryGetValue(scopeKey, out KeyValueEntry? keyValueEntry)
				&& keyValueEntry is not null
				&& keyValueEntry.ETag.Equals(request.Entry.ETag, StringComparison.Ordinal))
			{
				_scopedCache.Remove(scopeKey);
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}

		public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			_scopedCache.Clear();
			return Unit.Task;
		}

		public int GetCount()
		{
			return _scopedCache.Count();
		}

		private string MakeKey(string key)
		{
			return _scopedCache.ScopeName + key;
		}
	}
}
