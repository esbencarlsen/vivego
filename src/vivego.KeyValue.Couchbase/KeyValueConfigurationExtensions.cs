using Couchbase;
using Couchbase.Core.Retry;

using Microsoft.Extensions.DependencyInjection;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue.Couchbase;

public static class KeyValueConfigurationExtensions
{
	public static IServiceBuilder AddCouchbaseKeyValueStore(this IServiceCollection collection,
		string name,
		string connectionString,
		string bucketName,
		string scopeName,
		string collectionName)
	{
		if (collection is null) throw new ArgumentNullException(nameof(collection));
		if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
		if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Value cannot be null or empty.", nameof(connectionString));
		if (string.IsNullOrEmpty(bucketName)) throw new ArgumentException("Value cannot be null or empty.", nameof(bucketName));
		if (string.IsNullOrEmpty(collectionName)) throw new ArgumentException("Value cannot be null or empty.", nameof(collectionName));

		KeyValueStoreBuilder builder = new(name, collection);
		builder.Services.AddSingleton(_ =>
		{
			ClusterOptions clusterOptions = new ClusterOptions()
				.WithRetryStrategy(new BestEffortRetryStrategy())
				.WithConnectionString(connectionString)
				.WithCredentials("Administrator", "admin123");
			return Cluster.ConnectAsync(clusterOptions);
		});

		builder.Services.AddSingleton(new CouchbaseKeyValueStoreRequestHandlerOptions(bucketName, scopeName, collectionName));
		builder.Services.AddSingleton<CouchbaseKeyValueStoreRequestHandler>();
		builder.RegisterKeyValueStoreRequestHandler<CouchbaseKeyValueStoreRequestHandler>();

		return builder;
	}
}
