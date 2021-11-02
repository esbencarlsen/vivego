using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using vivego.Collection.Index.Model;
using vivego.Collection.Queue;

namespace vivego.Collection.Index
{
	public sealed class DefaultIndex : IIndex
	{
		private readonly IndexEntryFieldComparer _indexEntryFieldComparer;
		private readonly TimeSpan? _timeToLive;
		private readonly IQueue _queue;
		private readonly Func<string, IIndexCompactionStrategy> _compactionStrategyFactory;
		private readonly IMemoryCache _cache;
		private readonly TimeSpan _cacheTimeout;

		public DefaultIndex(
			IComparer<byte[]> byteArrayComparer,
			TimeSpan? timeToLive,
			TimeSpan cacheTimeout,
			IMemoryCache cache,
			IQueue queue,
			Func<string, IIndexCompactionStrategy> compactionStrategyFactory)
		{
			_timeToLive = timeToLive;
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_compactionStrategyFactory = compactionStrategyFactory ?? throw new ArgumentNullException(nameof(compactionStrategyFactory));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_cacheTimeout = cacheTimeout;
			_indexEntryFieldComparer = new IndexEntryFieldComparer(byteArrayComparer);
		}

		public async Task Add(string key, Value field, Value? data = default, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));
			if (field is null) throw new ArgumentNullException(nameof(field));

			ImmutableSortedSet<IIndexEntry> index = await GetIndex(key, cancellationToken).ConfigureAwait(false);
			DefaultIndexEntry indexEntry = new(field, data);
			if (!index.Contains(indexEntry))
			{
				ImmutableSortedSet<IIndexEntry> newIndex = index.Add(indexEntry);
				string cacheKey = MakeCacheKey(key);
				_cache.Set(cacheKey, newIndex, _cacheTimeout);
				Operations operations = new()
				{
					Add = true,
					Operation =
					{
						new Operation
						{
							Field = field.AsByteString,
							Data = data?.AsByteString ?? ByteString.Empty
						}
					}
				};
				await AddOperation(key, operations, cancellationToken).ConfigureAwait(false);
			}
		}

		public async Task Remove(string key, Value field, CancellationToken cancellationToken = default)
		{
			if (field is null) throw new ArgumentNullException(nameof(field));
			if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));
			ImmutableSortedSet<IIndexEntry> index = await GetIndex(key, cancellationToken).ConfigureAwait(false);
			DefaultIndexEntry indexEntry = new(field, Array.Empty<byte>());
			if (index.Contains(indexEntry))
			{
				ImmutableSortedSet<IIndexEntry> newIndex = index.Remove(indexEntry);
				string cacheKey = MakeCacheKey(key);
				_cache.Set(cacheKey, newIndex, _cacheTimeout);
				Operations operations = new()
				{
					Add = false,
					Operation =
					{
						new Operation
						{
							Field = field.AsByteString,
							Data = ByteString.Empty
						}
					}
				};
				await AddOperation(key, operations, cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task AddOperation(string key, IMessage message, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));
			byte[]? byteArray = message.ToByteArray();
			long? version = await _queue
				.Append(key, byteArray, default, _timeToLive, cancellationToken)
				.ConfigureAwait(false);

			if (version is not null
			    && GetIndexCompactionStrategy(key).DoCompaction(version.Value))
			{
				await Compact(key, cancellationToken).ConfigureAwait(false);
			}
		}

		private IIndexCompactionStrategy GetIndexCompactionStrategy(string key)
		{
			return _cache.GetOrCreate($"{nameof(DefaultIndex)}_compaction_{key}", _ =>
			{
				_.SlidingExpiration = _cacheTimeout;
				return _compactionStrategyFactory(key);
			});
		}

		private Task<ImmutableSortedSet<IIndexEntry>> GetIndex(string key, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));
			string cacheKey = MakeCacheKey(key);
			return _cache.GetOrCreateAsync(cacheKey, _ =>
			{
				_.SlidingExpiration = _cacheTimeout;
				return LoadFromStore(key, cancellationToken);
			});
		}

		public async Task<ImmutableSortedSet<IIndexEntry>> LoadFromStore(string key, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));
			ISet<IIndexEntry> set = new HashSet<IIndexEntry>(_indexEntryFieldComparer);
			await foreach (IQueueEntry queueEntry in _queue
				.GetAll(key, cancellationToken: cancellationToken)
				.ConfigureAwait(false))
			{
				Operations operations = Operations.Parser.ParseFrom(queueEntry.Data.Data);
				foreach (Operation operation in operations.Operation)
				{
					if (operations.Add)
					{
						set.Add(new DefaultIndexEntry(operation.Field.ToByteArray(), operation.Data.ToByteArray()));
					}
					else
					{
						set.Remove(new DefaultIndexEntry(operation.Field.ToByteArray(), Array.Empty<byte>()));
					}
				}
			}

			return set.ToImmutableSortedSet(_indexEntryFieldComparer);
		}

		public async Task Compact(string indexName, CancellationToken cancellationToken = default)
		{
			ImmutableSortedSet<IIndexEntry> list = await GetIndex(indexName, cancellationToken).ConfigureAwait(false);
			if (list.Count == 0)
			{
				await _queue
					.Truncate(indexName, cancellationToken: cancellationToken)
					.ConfigureAwait(false);
				return;
			}

			Operations operations = new() {Add = true};
			operations.Operation.AddRange(list.Select(pair => new Operation
			{
				Field = pair.Field.AsByteString,
				Data = pair.Data?.AsByteString ?? ByteString.Empty
			}));
			byte[]? byteArray = operations.ToByteArray();
			long? version = await _queue
				.Append(indexName, byteArray, default, _timeToLive, cancellationToken)
				.ConfigureAwait(false);
			await _queue
				.Truncate(indexName, version, version + 1, false, cancellationToken)
				.ConfigureAwait(false);
		}

		public Task<ImmutableSortedSet<IIndexEntry>> Get(string key, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));
			return GetIndex(key, cancellationToken);
		}

		private static string MakeCacheKey(string key)
		{
			return $"{nameof(DefaultIndex)}_{key}";
		}
	}
}
