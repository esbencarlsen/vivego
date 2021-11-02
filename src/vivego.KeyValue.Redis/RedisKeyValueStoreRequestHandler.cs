using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using StackExchange.Redis;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Redis
{
	public sealed class RedisKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly string _name;
		private readonly bool _skipETag;
		private readonly IDatabase _database;

		public RedisKeyValueStoreRequestHandler(
			string name,
			bool skipETag,
			IDatabase database)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_skipETag = skipETag;
			_database = database ?? throw new ArgumentNullException(nameof(database));
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry(_skipETag);
			await _database
				.HashSetAsync(_name, request.Entry.Key, keyValueEntry.ToByteArray())
				.ConfigureAwait(false);

			return keyValueEntry.ETag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

			RedisValue value = await _database
				.HashGetAsync(_name, request.Key)
				.ConfigureAwait(false);

			return value.HasValue
				? KeyValueEntry.Parser.ParseFrom(value)
				: KeyValueEntryExtensions.KeyValueNull;
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			bool success = await _database
				.HashDeleteAsync(_name, request.Entry.Key)
				.ConfigureAwait(false);

			return success;
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken) =>
			Task.FromResult(new KeyValueStoreFeatures
			{
				SupportsEtag = false,
				SupportsTtl = false,
				MaximumDataSize = 512 * 1024 * 1024,
				MaximumKeyLength = 512 * 1024 * 1024
			});

		public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			_database.Execute("FLUSHALL");
			return Unit.Task;
		}
	}
}
