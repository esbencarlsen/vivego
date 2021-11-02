using System;
using System.Text;

using Microsoft.Extensions.Caching.Memory;

namespace vivego.logger.core
{
	public sealed class MediaTypeEncodingResolver : IMediaTypeEncodingResolver
	{
		private readonly IMemoryCache _memoryCache;

		public MediaTypeEncodingResolver(IMemoryCache memoryCache)
		{
			_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		}

		public Encoding GetEncoding(string charSet)
		{
			if (string.IsNullOrEmpty(charSet))
			{
				return Encoding.UTF8;
			}

			return _memoryCache.GetOrCreate(nameof(MediaTypeEncodingResolver) + charSet, _ =>
			{
				_.SlidingExpiration = TimeSpan.FromMinutes(10);
				try
				{
					return Encoding.GetEncoding(charSet);
				}
				catch
				{
					return Encoding.UTF8;
				}
			});
		}
	}
}
