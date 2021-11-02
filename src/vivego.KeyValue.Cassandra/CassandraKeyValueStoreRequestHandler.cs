using System;
using System.Threading;
using System.Threading.Tasks;

using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

using Google.Protobuf;

using MediatR;

using Microsoft.Extensions.Logging;

using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Cassandra;

public sealed class CassandraKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
{
	private readonly bool _autoCreateKeyspace;
	private readonly ConsistencyLevel _consistencyLevel;
	private readonly bool _supportsEtag;
	private readonly ILogger<CassandraKeyValueStoreRequestHandler> _logger;
	private readonly Lazy<Task<Table<KeyValueEntry>>> _storageProviderEntries;
	private readonly Task<KeyValueStoreFeatures> _features;
	private readonly bool _supportsTtl;

	public CassandraKeyValueStoreRequestHandler(
		string cassandraConnectionString,
		bool autoCreateKeyspace,
		string tableName,
		ConsistencyLevel consistencyLevel,
		bool supportsEtag,
		ILogger<CassandraKeyValueStoreRequestHandler> logger)
	{
		if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));
		if (string.IsNullOrEmpty(cassandraConnectionString)) throw new ArgumentException("Value cannot be null or empty.", nameof(cassandraConnectionString));
		_autoCreateKeyspace = autoCreateKeyspace;
		_consistencyLevel = consistencyLevel;
		_supportsEtag = supportsEtag;
		_supportsTtl = cassandraConnectionString.SupportsTtl();
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_features = Task.FromResult(new KeyValueStoreFeatures
		{
			SupportsEtag = _supportsEtag,
			SupportsTtl = true,
			MaximumDataSize = 1024 * 1024,
			MaximumKeyLength = 65535
		});

		_storageProviderEntries = new Lazy<Task<Table<KeyValueEntry>>>(() => Initialize(cassandraConnectionString, tableName), true);
	}

	public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
	{
		return _features;
	}

	public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
	{
#pragma warning disable CA2208
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
#pragma warning restore CA2208
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));

		Abstractions.Model.KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry(!_supportsEtag);
		string key = request.Entry.Key;
		DateTimeOffset? expiresAt = default;
		if (keyValueEntry.ExpiresAtUnixTimeSeconds > 0)
		{
			expiresAt = DateTimeOffset.FromUnixTimeSeconds(keyValueEntry.ExpiresAtUnixTimeSeconds);
		}

		byte[] data = keyValueEntry.ToByteArray();
		Table<KeyValueEntry> table = await _storageProviderEntries.Value.ConfigureAwait(false);
		if (_supportsEtag && !string.IsNullOrEmpty(request.Entry.ETag))
		{
			string etag = request.Entry.ETag;
			CqlConditionalCommand<KeyValueEntry> cqlUpdate = table
				.Where(providerEntry => providerEntry.Id == key)
				.SetConsistencyLevel(_consistencyLevel)
				.Select(providerEntry => new KeyValueEntry
				{
					Data = data,
					ETag = keyValueEntry.ETag,
					ExpiresAt = expiresAt
				})
				.UpdateIf(entry => entry.ETag == etag);

			if (request.Entry.ExpiresInSeconds > 0 && _supportsTtl)
			{
				cqlUpdate = cqlUpdate.SetTTL((int)request.Entry.ExpiresInSeconds);
			}

			AppliedInfo<KeyValueEntry> appliedInfo = await cqlUpdate
				.LogAndExecuteAsync(_logger)
				.ConfigureAwait(false);
			return appliedInfo.Applied ? keyValueEntry.ETag : string.Empty;
		}

		CqlCommand cqlCommand = table
			.Where(providerEntry => providerEntry.Id == key)
			.SetConsistencyLevel(_consistencyLevel)
			.Select(providerEntry => new KeyValueEntry
			{
				Data = data,
				ETag = keyValueEntry.ETag,
				ExpiresAt = expiresAt
			})
			.Update();

		if (request.Entry.ExpiresInSeconds > 0 && _supportsTtl)
		{
			cqlCommand = cqlCommand.SetTTL((int)request.Entry.ExpiresInSeconds);
		}

		await cqlCommand
			.LogAndExecuteAsync(_logger, "Update")
			.ConfigureAwait(false);

		return keyValueEntry.ETag;
	}

	public async Task<Abstractions.Model.KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
	{
#pragma warning disable CA2208
		if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
#pragma warning restore CA2208

		Table<KeyValueEntry> table = await _storageProviderEntries.Value.ConfigureAwait(false);
		KeyValueEntry keyValueEntry = await table
			.FirstOrDefault(providerEntry => providerEntry.Id == request.Key)
			.LogAndExecuteAsync(_logger)
			.ConfigureAwait(false);

		if (keyValueEntry?.Data is null)
		{
			return KeyValueEntryExtensions.KeyValueNull;
		}

		return Abstractions.Model.KeyValueEntry.Parser.ParseFrom(keyValueEntry.Data);
	}

	public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
	{
#pragma warning disable CA2208
		if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
#pragma warning restore CA2208
		if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));

		Table<KeyValueEntry> table = await _storageProviderEntries.Value.ConfigureAwait(false);
		string key = request.Entry.Key;
		if (_supportsEtag && !string.IsNullOrEmpty(request.Entry.ETag))
		{
			string etag = request.Entry.ETag;
			AppliedInfo<KeyValueEntry> appliedInfo = await table
				.Where(providerEntry => providerEntry.Id == key)
				.SetConsistencyLevel(_consistencyLevel)
				.DeleteIf(entry => entry.ETag == etag)
				.LogAndExecuteAsync(_logger)
				.ConfigureAwait(false);
			return appliedInfo.Applied;
		}

		await table
			.Where(providerEntry => providerEntry.Id == key)
			.SetConsistencyLevel(_consistencyLevel)
			.Delete()
			.LogAndExecuteAsync(_logger)
			.ConfigureAwait(false);
		return true;
	}

	public async Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
	{
		Table<KeyValueEntry> table = await _storageProviderEntries.Value.ConfigureAwait(false);
		await table
			.Delete()
			.LogAndExecuteAsync(_logger)
			.ConfigureAwait(false);
		return Unit.Value;
	}

	private async Task<Table<KeyValueEntry>> Initialize(string cassandraConnectionString, string tableName)
	{
		if (_autoCreateKeyspace)
		{
			CassandraConnectionStringBuilder builder = new(cassandraConnectionString);
			string keyspace = builder.DefaultKeyspace;
			if (!string.IsNullOrEmpty(keyspace))
			{
				builder.DefaultKeyspace = string.Empty;
				ISession keyspaceSession = await CassandraSessionMaker.Instance
					.MakeSession(builder.ToString(), _logger)
					.ConfigureAwait(false);
				keyspaceSession.CreateKeyspaceIfNotExists(keyspace);
			}
		}

		ISession session = await CassandraSessionMaker.Instance
			.MakeSession(cassandraConnectionString, _logger)
			.ConfigureAwait(false);

		MappingConfiguration config = new();
		config.Define(new Map<KeyValueEntry>().PartitionKey(u => u.Id));
		Table<KeyValueEntry> table = new Table<KeyValueEntry>(session, config, tableName);
		await table
			.CreateIfNotExistsEx()
			.ConfigureAwait(false);

		return table;
	}
}
