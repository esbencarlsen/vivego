using System;
using System.Collections.Generic;

using Microsoft.Extensions.Caching.Memory;

namespace vivego.core
{
	public sealed class ScopedCache : DisposableBase, IMemoryCache
	{
		private readonly TimeSpan _scopeCacheTimeout;
		private readonly IMemoryCache _innerCache;
		private readonly object _scopeLock = new();

		public string ScopeName { get; }

		public ScopedCache(
			string scopeName,
			TimeSpan scopeCacheTimeout,
			IMemoryCache innerCache)
		{
			if (string.IsNullOrEmpty(scopeName)) throw new ArgumentException("Value cannot be null or empty.", nameof(scopeName));
			ScopeName = scopeName;
			_scopeCacheTimeout = scopeCacheTimeout;
			_innerCache = innerCache ?? throw new ArgumentNullException(nameof(innerCache));
		}

		private HashSet<string> GetHashSet()
		{
			return _innerCache.GetOrCreate(ScopeName, _ =>
			{
				_.SlidingExpiration = _scopeCacheTimeout;
				return new HashSet<string>(StringComparer.Ordinal);
			});
		}

		public void Clear()
		{
			foreach (string key in GetHashSet())
			{
				_innerCache.Remove(key);
			}

			_innerCache.Remove(ScopeName);
		}

		public int Count()
		{
			lock (_scopeLock)
			{
				return GetHashSet().Count;
			}
		}

		public bool TryGetValue(object key, out object value)
		{
			return _innerCache.TryGetValue(key, out value);
		}

		public ICacheEntry CreateEntry(object key)
		{
			if (key is null) throw new ArgumentNullException(nameof(key));
			lock (_scopeLock)
			{
				GetHashSet().Add(key.ToString() ?? string.Empty);
			}

			return _innerCache.CreateEntry(key);
		}

		public void Remove(object key)
		{
			if (key is null) throw new ArgumentNullException(nameof(key));
			lock (_scopeLock)
			{
				GetHashSet().Remove(key.ToString() ?? string.Empty);
			}

			_innerCache.Remove(key);
		}
	}
}
