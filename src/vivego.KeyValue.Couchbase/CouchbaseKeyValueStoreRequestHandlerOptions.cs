namespace vivego.KeyValue.Couchbase;

public sealed record CouchbaseKeyValueStoreRequestHandlerOptions(string BucketName, string ScopeName, string CollectionName);
