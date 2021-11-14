namespace vivego.KeyValue.DynamoDb
{
	public sealed record DynamoDbKeyValueStoreRequestHandlerConfig(
		string TableName,
		bool SupportsEtag);
}
