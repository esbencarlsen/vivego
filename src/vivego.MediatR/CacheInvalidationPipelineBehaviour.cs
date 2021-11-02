using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Caching.Memory;

namespace vivego.MediatR
{
	internal sealed class CacheInvalidationPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
	{
		private readonly Func<TRequest, string> _cacheKeyGenerator;
		private readonly IMemoryCache _memoryCache;

		public CacheInvalidationPipelineBehaviour(
			Func<TRequest, string> cacheKeyGenerator,
			IMemoryCache memoryCache)
		{
			_cacheKeyGenerator = cacheKeyGenerator ?? throw new ArgumentNullException(nameof(cacheKeyGenerator));
			_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		}

		public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
			string cacheKey = _cacheKeyGenerator(request);
			_memoryCache.Remove(cacheKey);
			return next();
		}
	}
}