using System.Globalization;

using Couchbase;
using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.KeyValue;

using Google.Protobuf;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Couchbase;

public readonly record struct DataDocument(byte[] Data);

public sealed class CouchbaseKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
{
	private readonly Task<KeyValueStoreFeatures> _features;
	private readonly Lazy<Task<ICouchbaseCollection>> _collection;

	public CouchbaseKeyValueStoreRequestHandler(
		Task<ICluster> clusterTask,
		CouchbaseKeyValueStoreRequestHandlerOptions options)
	{
		if (clusterTask is null) throw new ArgumentNullException(nameof(clusterTask));
		if (options is null) throw new ArgumentNullException(nameof(options));

		_features = Task.FromResult(new KeyValueStoreFeatures
		{
			SupportsEtag = true,
			SupportsTtl = true,
			MaximumDataSize = 1024L * 1024L * 20, // 20Mb
			MaximumKeyLength = 250
		});

		_collection = new Lazy<Task<ICouchbaseCollection>>(async () =>
		{
			ICluster cluster = await clusterTask.ConfigureAwait(false);
			IBucket bucket = await cluster.BucketAsync(options.BucketName).ConfigureAwait(false);
			IScope scope = await bucket.ScopeAsync(options.ScopeName).ConfigureAwait(false);
			ICouchbaseCollection collection = await scope.CollectionAsync(options.CollectionName).ConfigureAwait(false);
			return collection;
		}, true);
	}

	public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
	{
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

		ICouchbaseCollection collection = await _collection.Value.ConfigureAwait(false);
		KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry(true);

		DataDocument content = new(keyValueEntry.ToByteArray());

		IMutationResult mutationResult;
		if (!string.IsNullOrEmpty(request.Entry.ETag))
		{
			if (ulong.TryParse(request.Entry.ETag, out ulong cas))
			{
				ReplaceOptions replaceOptions = new ReplaceOptions()
					.Cas(cas)
					.CancellationToken(cancellationToken);
				if (request.Entry.ExpiresInSeconds > 0)
				{
					replaceOptions.Expiry(TimeSpan.FromSeconds(request.Entry.ExpiresInSeconds));
				}

				mutationResult = await collection
					.ReplaceAsync(request.Entry.Key, content, replaceOptions)
					.ConfigureAwait(false);
			}
			else
			{
				return string.Empty;
			}
		}
		else
		{
			UpsertOptions upsertOptions = new UpsertOptions().CancellationToken(cancellationToken);
			if (request.Entry.ExpiresInSeconds > 0)
			{
				upsertOptions.Expiry(TimeSpan.FromSeconds(request.Entry.ExpiresInSeconds));
			}

			mutationResult = await collection
				.UpsertAsync(request.Entry.Key, content, upsertOptions)
				.ConfigureAwait(false);
		}

		return mutationResult.Cas.ToString(CultureInfo.InvariantCulture);
	}

	public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

		try
		{
			ICouchbaseCollection collection = await _collection.Value.ConfigureAwait(false);
			IGetResult getResult = await collection
				.GetAsync(request.Key)
				.ConfigureAwait(false);

			DataDocument data = getResult.ContentAs<DataDocument>();
			KeyValueEntry keyValueEntry = KeyValueEntry.Parser.ParseFrom(data.Data);
			if (keyValueEntry.ExpiresAtUnixTimeSeconds > 0
				&& DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(keyValueEntry.ExpiresAtUnixTimeSeconds))
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}

			return keyValueEntry;
		}
		catch (DocumentNotFoundException)
		{
			return KeyValueEntryExtensions.KeyValueNull;
		}
	}

	public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
	{
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

		ICouchbaseCollection collection = await _collection.Value.ConfigureAwait(false);

		RemoveOptions removeOptions = new RemoveOptions().CancellationToken(cancellationToken);
		if (!string.IsNullOrEmpty(request.Entry.ETag))
		{
			if (ulong.TryParse(request.Entry.ETag, out ulong cas))
			{
				removeOptions.Cas(cas);
			}
			else
			{
				return false;
			}
		}

		try
		{
			await collection
				.RemoveAsync(request.Entry.Key, removeOptions)
				.ConfigureAwait(false);
		}
		catch (InvalidArgumentException)
		{
			return false;
		}

		return true;
	}

	public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
	{
		return _features;
	}

	public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
