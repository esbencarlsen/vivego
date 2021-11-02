using System;

using Cassandra;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.KeyValue.Cassandra;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue;

public static class KeyValueConfigurationExtensions
{
	public static IServiceBuilder AddCassandraKeyValueStore(this IServiceCollection collection,
		string providerName,
		string cassandraConnectionString,
		bool autoCreateKeyspace,
		string tableName,
		ConsistencyLevel consistencyLevel,
		bool supportEtag)
	{
		if (collection is null) throw new ArgumentNullException(nameof(collection));
		if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));
		if (string.IsNullOrEmpty(cassandraConnectionString)) throw new ArgumentException("Value cannot be null or empty.", nameof(cassandraConnectionString));
		if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));

		KeyValueStoreBuilder builder = new(providerName, collection);
		builder.RegisterKeyValueStoreRequestHandler<CassandraKeyValueStoreRequestHandler>();
		builder.DependsOn<ILogger<CassandraKeyValueStoreRequestHandler>>();
		builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<CassandraKeyValueStoreRequestHandler>(sp,
			cassandraConnectionString,
			autoCreateKeyspace,
			tableName,
			consistencyLevel,
			supportEtag));
		return builder;
	}
}
