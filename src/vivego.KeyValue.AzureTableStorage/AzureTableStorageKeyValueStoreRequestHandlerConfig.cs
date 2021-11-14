namespace vivego.KeyValue.AzureTableStorage;

public sealed record AzureTableStorageKeyValueStoreRequestHandlerConfig(
	string AzureTableStorageConnectionString,
	string TableName);
