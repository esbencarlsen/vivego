using System;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.AzureTableStorage;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue;

public static class KeyValueConfigurationExtensions
{
	public static IServiceBuilder AddAzureTableStorageKeyValueStore(this IServiceCollection collection,
		string providerName,
		string azureTableStorageConnectionString,
		string tableName)
	{
		if (collection is null) throw new ArgumentNullException(nameof(collection));
		if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));
		if (string.IsNullOrEmpty(azureTableStorageConnectionString)) throw new ArgumentException("Value cannot be null or empty.", nameof(azureTableStorageConnectionString));
		if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));

		KeyValueStoreBuilder builder = new(providerName, collection);

		AzureTableStorageKeyValueStoreRequestHandlerConfig config = new(
			azureTableStorageConnectionString,
			tableName);
		builder.Services.AddSingleton(config);

		builder.Services.AddSingleton<AzureTableStorageKeyValueStoreRequestHandler>();
		builder.RegisterKeyValueStoreRequestHandler<AzureTableStorageKeyValueStoreRequestHandler>();

		return builder;
	}
}
