using System;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using vivego.KeyValue.Redis;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddRedisKeyValueStore(this IServiceCollection collection,
			string providerName,
			string connectionString,
			string hashSetName,
			bool skipETag,
			int databaseNumber = -1)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));
			if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Value cannot be null or empty.", nameof(connectionString));
			if (string.IsNullOrEmpty(hashSetName)) throw new ArgumentException("Value cannot be null or empty.", nameof(hashSetName));

			KeyValueStoreBuilder builder = new(providerName, collection);
			builder.RegisterKeyValueStoreRequestHandler<RedisKeyValueStoreRequestHandler>();
			builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisKeyValueStoreRequestHandler>(sp, hashSetName, skipETag));

			builder.Services.AddSingleton(_ =>
			{
				IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
				return redis;
			});

			builder.Services.AddSingleton(sp =>
			{
				IConnectionMultiplexer redis = sp.GetRequiredService<IConnectionMultiplexer>();
				IDatabase database = redis.GetDatabase(databaseNumber);
				return database;
			});

			return builder;
		}
	}
}
