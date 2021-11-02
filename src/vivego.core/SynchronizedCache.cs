using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

namespace vivego.core;

public sealed class SynchronizedCache
{
	private readonly AsyncSemaphoreFactory _lockProvider;
	private readonly IMemoryCache _memoryCache;

	public IMemoryCache MemoryCache => _memoryCache;

	public SynchronizedCache(
		AsyncSemaphoreFactory lockProvider,
		IMemoryCache memoryCache)
	{
		_lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	}

	public Task<T> GetOrCreate<T>(string key, Func<Task<T>> factory, MemoryCacheEntryOptions cacheEntryOptions)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));

		if (_memoryCache.TryGetValue(key, out Lazy<Task<T>>? cachedValue) && cachedValue is not null)
		{
			return cachedValue.Value;
		}

		return _lockProvider
			.Get(key)
			.WaitAsync(() =>
			{
				if (_memoryCache.TryGetValue(key, out cachedValue) && cachedValue is not null)
				{
					return cachedValue.Value;
				}

				Lazy<Task<T>> lazy = new(factory);
				_memoryCache.Set(key, lazy, cacheEntryOptions);
				return lazy.Value;
			});
	}

	public bool TryGetValue<T>(string key, out Task<T>? value)
	{
		if (string.IsNullOrEmpty(key)) throw new ArgumentException("Value cannot be null or empty.", nameof(key));

		if (_memoryCache.TryGetValue(key, out Lazy<Task<T>>? cachedValue) && cachedValue is not null)
		{
			value = cachedValue.Value;
			return true;
		}

		value = default;
		return false;
	}

	public void Set<T>(string key, T value, MemoryCacheEntryOptions cacheEntryOptions)
	{
		_memoryCache.Set(key, new Lazy<Task<T>>(() => Task.FromResult(value)), cacheEntryOptions);
	}

	public void Remove(string key)
	{
		_memoryCache.Remove(key);
	}
}
