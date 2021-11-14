using System;

using Amazon.DynamoDBv2;
using Amazon.Runtime;

using Microsoft.Extensions.DependencyInjection;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue.DynamoDb
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddDynamoDbKeyValueStore(this IServiceCollection collection,
			string name,
			string tableName,
			bool supportsEtag,
			AWSCredentials credentials,
			AmazonDynamoDBConfig clientConfig)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (credentials is null) throw new ArgumentNullException(nameof(credentials));
			if (clientConfig is null) throw new ArgumentNullException(nameof(clientConfig));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
			{
				AmazonDynamoDBClient client = new(credentials, clientConfig);
				return client;
			});

			builder.Services.AddSingleton(new DynamoDbKeyValueStoreRequestHandlerConfig(tableName, supportsEtag));
			builder.Services.AddSingleton<DynamoDbKeyValueStoreRequestHandler>();

			builder.RegisterKeyValueStoreRequestHandler<DynamoDbKeyValueStoreRequestHandler>();

			return builder;
		}
	}
}
