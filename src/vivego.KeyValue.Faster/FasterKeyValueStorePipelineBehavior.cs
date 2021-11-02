using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using Microsoft.Extensions.Hosting;

using vivego.core;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Set;
using vivego.Microsoft.Faster.PersistentLog;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue.Faster
{
	public sealed class FasterKeyValueStorePipelineBehavior : BackgroundService,
		IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>,
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<DeleteRequest, bool>
	{
		private bool _stopping;
		private readonly IKeyValueStore _keyValueStore;
		private readonly FasterPersistentLog[] _fasterLogs;

		public FasterKeyValueStorePipelineBehavior(
			IServiceManager<FasterPersistentLog> fasterLogServiceManager,
			IKeyValueStore keyValueStore)
		{
			if (fasterLogServiceManager is null) throw new ArgumentNullException(nameof(fasterLogServiceManager));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));

			_fasterLogs = fasterLogServiceManager.GetAll().ToArray();
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));

			if (_stopping)
			{
				return await next().ConfigureAwait(false);
			}

			if (request.Entry.MetaData.ContainsKey(nameof(FasterKeyValueStorePipelineBehavior)))
			{
				request.Entry.MetaData.Remove(nameof(FasterKeyValueStorePipelineBehavior));
				return await next().ConfigureAwait(false);
			}

			request.Entry.MetaData[nameof(FasterKeyValueStorePipelineBehavior)] = ByteString.Empty;
			Transaction transaction = new()
			{
				Set = request.Entry
			};

			FasterPersistentLog log = _fasterLogs[request.Entry.Key.GetDeterministicHashCode() % _fasterLogs.Length];
			await log
				.Write(transaction.ToByteArray(), cancellationToken)
				.ConfigureAwait(false);

			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry();
			return keyValueEntry.ETag;
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));

			if (_stopping)
			{
				return await next().ConfigureAwait(false);
			}

			if (request.Entry.MetaData.ContainsKey(nameof(FasterKeyValueStorePipelineBehavior)))
			{
				request.Entry.MetaData.Remove(nameof(FasterKeyValueStorePipelineBehavior));
				return await next().ConfigureAwait(false);
			}

			request.Entry.MetaData[nameof(FasterKeyValueStorePipelineBehavior)] = ByteString.Empty;
			Transaction transaction = new()
			{
				Delete = request.Entry
			};

			FasterPersistentLog log = _fasterLogs[request.Entry.Key.GetDeterministicHashCode() % _fasterLogs.Length];
			await log
				.Write(transaction.ToByteArray(), cancellationToken)
				.ConfigureAwait(false);
			return true;
		}

		private async Task Persist(Transaction transaction, CancellationToken cancellationToken)
		{
			if (transaction.Set is not null)
			{
				await _keyValueStore
					.Set(transaction.Set, cancellationToken)
					.ConfigureAwait(false);
			}

			if (transaction.Delete is not null)
			{
				await _keyValueStore
					.Delete(transaction.Delete, cancellationToken)
					.ConfigureAwait(false);
			}
		}

		public async Task<KeyValueStoreFeatures> Handle(FeaturesRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<KeyValueStoreFeatures> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			KeyValueStoreFeatures result = await next().ConfigureAwait(false);
			result.SupportsEtag = false;
			return result;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Task[] logReaderTasks = _fasterLogs
				.Select(fasterLog => LogReader(fasterLog, stoppingToken))
				.ToArray();
			CancellationTokenRegistration registration = stoppingToken.Register(() => _stopping = true);
			await using ConfiguredAsyncDisposable _ = registration.ConfigureAwait(false);
			try
			{
				await Task.WhenAll(logReaderTasks).ConfigureAwait(false);
			}
			finally
			{
				foreach (FasterPersistentLog log in _fasterLogs)
				{
					await log.DisposeAsync().ConfigureAwait(false);
				}
			}
		}

		private async Task LogReader(FasterPersistentLog fasterLog,
			CancellationToken stoppingToken)
		{
			await foreach (byte[] bytes in fasterLog
				.Subscribe(cancellationToken: stoppingToken)
				.ConfigureAwait(false))
			{
				if (stoppingToken.IsCancellationRequested)
				{
					break;
				}

				Transaction transaction = Transaction.Parser.ParseFrom(bytes);
				await Persist(transaction, stoppingToken).ConfigureAwait(false);
			}
		}
	}
}
