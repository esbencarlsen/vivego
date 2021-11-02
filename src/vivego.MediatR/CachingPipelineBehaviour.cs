using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Caching.Memory;

using vivego.core;

namespace vivego.MediatR
{
	internal sealed class CachingPipelineBehaviour<TRequest, TResponse> :
		IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly Func<TRequest, string> _cacheKeyGenerator;
		private readonly Action<IServiceProvider, ICacheEntry> _setCacheTimeout;
		private readonly Func<TResponse?, bool> _shouldCache;
		private readonly IMemoryCache _memoryCache;
		private readonly AsyncSemaphoreFactory _lockProvider;

		public CachingPipelineBehaviour(
			IServiceProvider serviceProvider,
			IMemoryCache memoryCache,
			AsyncSemaphoreFactory lockProvider,
			Func<TRequest, string> cacheKeyGenerator,
			Action<IServiceProvider, ICacheEntry> setCacheTimeout,
			Func<TResponse?, bool> shouldCache)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheKeyGenerator = cacheKeyGenerator ?? throw new ArgumentNullException(nameof(cacheKeyGenerator));
			_setCacheTimeout = setCacheTimeout ?? throw new ArgumentNullException(nameof(setCacheTimeout));
			_shouldCache = shouldCache ?? throw new ArgumentNullException(nameof(shouldCache));
			_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
			_lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
		}

		public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
			string cacheKey = _cacheKeyGenerator(request);
			if (_memoryCache.TryGetValue(cacheKey, out TResponse result))
			{
				return Task.FromResult(result);
			}

			return _lockProvider
				.Get(cacheKey)
				.WaitAsync(async () =>
				{
					if (_memoryCache.TryGetValue(cacheKey, out result))
					{
						return result;
					}

					result = await next().ConfigureAwait(false);
					if (_shouldCache(result))
					{
						using ICacheEntry entry = _memoryCache.CreateEntry(cacheKey);
						entry.Value = result;
						_setCacheTimeout(_serviceProvider, entry);
					}

					return result;
				}, cancellationToken);
		}
	}
}
