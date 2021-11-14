using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Data.Tables;

using Google.Protobuf;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.AzureTableStorage;

public sealed class AzureTableStorageKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
{
	private readonly AzureTableStorageKeyValueStoreRequestHandlerConfig _options;
	private readonly Task<KeyValueStoreFeatures> _features;
	private readonly Lazy<Task<TableClient>> _tableClient;

	public AzureTableStorageKeyValueStoreRequestHandler(AzureTableStorageKeyValueStoreRequestHandlerConfig options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));

		_tableClient = new Lazy<Task<TableClient>>(GetCloudTable, true);

		_features = Task.FromResult(new KeyValueStoreFeatures
		{
			SupportsEtag = true,
			SupportsTtl = false,
			MaximumDataSize = 1024L * 1024L, // 1Mb
			MaximumKeyLength = 1024
		});
	}

	public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
	{
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

		KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry(true);
		TableEntity entity = new(request.Entry.Key, request.Entry.Key)
		{
			{ "Data", keyValueEntry.ToByteArray() }
		};

		if (request.Entry.ExpiresInSeconds > 0)
		{
			entity["ExpiresAt"] = DateTimeOffset.UtcNow;
		}

		Response response;
		TableClient tableClient = await _tableClient.Value.ConfigureAwait(false);
		try
		{
			if (string.IsNullOrEmpty(request.Entry.ETag))
			{
				response = await tableClient
					.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken)
					.ConfigureAwait(false);
			}
			else
			{
				response = await tableClient
					.UpdateEntityAsync(entity, new ETag(request.Entry.ETag), TableUpdateMode.Replace, cancellationToken)
					.ConfigureAwait(false);
			}
		}
		catch (RequestFailedException)
		{
			return string.Empty;
		}

		return response.Headers.ETag.ToString() ?? string.Empty;
	}

	public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

		TableClient tableClient = await _tableClient.Value.ConfigureAwait(false);

		try
		{
			Response<TableEntity> response = await tableClient
				.GetEntityAsync<TableEntity>(request.Key, request.Key, default, cancellationToken)
				.ConfigureAwait(false);

			if (response.Value.TryGetValue("Data", out object? dataObject)
				&& dataObject is byte[] data)
			{
				KeyValueEntry entry = KeyValueEntry.Parser.ParseFrom(data);
				entry.ETag = response.Value.ETag.ToString();
				return entry;
			}
		}
		catch (RequestFailedException)
		{
			// Ignore
		}

		return KeyValueEntryExtensions.KeyValueNull;
	}

	public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
	{
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

		TableClient tableClient = await _tableClient.Value.ConfigureAwait(false);

		try
		{
			Response response;
			if (string.IsNullOrEmpty(request.Entry.ETag))
			{
				response = await tableClient
					.DeleteEntityAsync(request.Entry.Key,
						request.Entry.Key,
						ETag.All,
						cancellationToken)
					.ConfigureAwait(false);
			}
			else
			{
				response = await tableClient
					.DeleteEntityAsync(request.Entry.Key,
						request.Entry.Key,
						new ETag(request.Entry.ETag),
						cancellationToken)
					.ConfigureAwait(false);
			}

			return response.Status == 204;
		}
		catch (RequestFailedException requestFailedException)
		{
			Console.Out.WriteLine(requestFailedException);
			return false;
		}
	}

	public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
	{
		return _features;
	}

	public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	private async Task<TableClient> GetCloudTable()
	{
		TableClient tableClient = new(_options.AzureTableStorageConnectionString, _options.TableName);
		await tableClient.CreateIfNotExistsAsync().ConfigureAwait(false);
		return tableClient;
	}
}
