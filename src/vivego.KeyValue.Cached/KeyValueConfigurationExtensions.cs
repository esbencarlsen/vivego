using System;

using Microsoft.Extensions.Caching.Memory;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;
using vivego.MediatR;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddCachingKeyValueStoreBehaviour(this IServiceBuilder builder,
			Action<IServiceProvider, ICacheEntry> setCacheTimeout,
			string? cachePrefix = default)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.AddCachingPipelineBehaviour<GetRequest, KeyValueEntry>(request =>
			{
				if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
				return cachePrefix is not null ? cachePrefix + request.Key : request.Key;
			}, setCacheTimeout, entry => entry is not null && !entry.Value.IsNull());
			builder.AddCacheInvalidationPipelineBehaviour<SetRequest, string>(request =>
			{
				if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));
				return cachePrefix is not null ? cachePrefix + request.Entry.Key : request.Entry.Key;
			});
			builder.AddCacheInvalidationPipelineBehaviour<DeleteRequest, bool>(request =>
			{
				if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));
				return cachePrefix is not null ? cachePrefix + request.Entry.Key : request.Entry.Key;
			});

			return builder;
		}
	}
}
