using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using Microsoft.EntityFrameworkCore;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Sql
{
	public sealed class SqlKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly Func<StateDbContext> _stateDbContextFactory;
		private readonly Task<KeyValueStoreFeatures> _features;

		public SqlKeyValueStoreRequestHandler(Func<StateDbContext> stateDbContextFactory)
		{
			_stateDbContextFactory = stateDbContextFactory ?? throw new ArgumentNullException(nameof(stateDbContextFactory));

			_features = Task.FromResult(new KeyValueStoreFeatures
			{
				SupportsEtag = true,
				SupportsTtl = false,
				MaximumDataSize = 1024 * 1024 * 1024, // 1GB
				MaximumKeyLength = 1024 * 1024 * 1024 // 1GB
			});
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken) => _features;

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry();
			StateDbContext stateDbContext = _stateDbContextFactory();
			await using ConfiguredAsyncDisposable _ = stateDbContext.ConfigureAwait(false);

			StateEntry? exists = await stateDbContext.States
				.FindAsync(new object[] { request.Entry.Key }, cancellationToken)
				.ConfigureAwait(false);
			if (exists is null)
			{
				StateEntry stateEntry = new()
				{
					Id = request.Entry.Key,
					Data = keyValueEntry.ToByteArray(),
					Etag = keyValueEntry.ETag,
					ExpiresAt = request.Entry.ExpiresInSeconds <= 0
						? default
						: DateTimeOffset.UtcNow.AddSeconds(request.Entry.ExpiresInSeconds)
				};

				await stateDbContext.States
					.AddAsync(stateEntry, cancellationToken)
					.ConfigureAwait(false);
			}
			else
			{
				if (!string.IsNullOrEmpty(request.Entry.ETag) && !exists.Etag.Equals(request.Entry.ETag, StringComparison.Ordinal))
				{
					return string.Empty;
				}

				exists.Data = keyValueEntry.ToByteArray();
				exists.Etag = keyValueEntry.ETag;
				stateDbContext.States.Update(exists);
			}

			// Attempt to save changes to the database
			await stateDbContext
				.SaveChangesAsync(cancellationToken)
				.ConfigureAwait(false);

			return keyValueEntry.ETag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

			StateDbContext stateDbContext = _stateDbContextFactory();
			await using ConfiguredAsyncDisposable _ = _stateDbContextFactory().ConfigureAwait(false);
			StateEntry? stateEntry = await stateDbContext
				.States
				.AsNoTracking()
				.Where(entry => entry.Id == request.Key)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);

			if (stateEntry?.Data is null)
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}

			if (stateEntry.ExpiresAt is not null
				&& stateEntry.ExpiresAt > DateTimeOffset.MinValue
				&& stateEntry.ExpiresAt < DateTimeOffset.UtcNow)
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}

			return KeyValueEntry.Parser.ParseFrom(stateEntry.Data);
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			StateDbContext stateDbContext = _stateDbContextFactory();
			await using ConfiguredAsyncDisposable _ = stateDbContext.ConfigureAwait(false);

			if (string.IsNullOrEmpty(request.Entry.ETag))
			{
				stateDbContext.States.Remove(new StateEntry
				{
					Id = request.Entry.Key
				});
			}
			else
			{
				StateEntry? stateEntry = await stateDbContext.States
					.FindAsync(new object[] { request.Entry.Key }, cancellationToken)
					.ConfigureAwait(false);
				if (stateEntry is null)
				{
					return true;
				}

				if (stateEntry.Etag.Equals(request.Entry.ETag, StringComparison.Ordinal))
				{
					stateDbContext.States.Remove(stateEntry);
				}
				else
				{
					return false;
				}
			}

			// Attempt to save changes to the database
			int rowsUpdated = await stateDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
			return rowsUpdated > 0;
		}

		public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
	}
}
