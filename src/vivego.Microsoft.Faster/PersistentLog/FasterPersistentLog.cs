using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using FASTER.core;

using Microsoft.Extensions.Logging;

using vivego.core;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Microsoft.Faster.PersistentLog
{
	public sealed class FasterPersistentLog : AsyncDisposableBase, INamedService
	{
		private readonly bool _flushOnStop;
		private readonly ILogger _logger;
		private readonly IDevice _device;

		public FasterLog FasterLog { get; }

		public FasterPersistentLog(
			string name,
			FileInfo logDirectory,
			bool flushOnStop,
			bool deleteOnClose,
			ILogger logger)
		{
			if (logDirectory is null) throw new ArgumentNullException(nameof(logDirectory));

			Name = name ?? throw new ArgumentNullException(nameof(name));
			_flushOnStop = flushOnStop;
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			if (deleteOnClose)
			{
				// ReSharper disable once EmptyGeneralCatchClause
				try { logDirectory.Directory?.Delete(true); }
				catch { }
			}

			if (!logDirectory.Exists)
			{
				logDirectory.Directory?.Create();
			}

			_logger.LogDebug("Using FASTER log: {LogDirectory}", logDirectory.FullName);
			_device = Devices.CreateLogDevice(logDirectory.FullName, deleteOnClose: deleteOnClose);
			FasterLog = new FasterLog(new FasterLogSettings
			{
				LogDevice = _device
			});

			RegisterDisposable(async () =>
			{
				await FasterLog.CommitAsync(CancellationToken.None).ConfigureAwait(false);
				FasterLog.Dispose();
				if (deleteOnClose)
				{
					// ReSharper disable once EmptyGeneralCatchClause
					try { logDirectory.Directory?.Delete(true); }
					catch { }
				}
			});
		}

		public string Name { get; }

		public async Task Write(byte[] data, CancellationToken cancellationToken = default)
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("Already disposed");
			}

			await FasterLog
				.EnqueueAsync(data, cancellationToken)
				.ConfigureAwait(false);
			FasterLog.RefreshUncommitted();
		}

		public FasterLogScanIterator GetIterator(string? iteratorName = default)
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("Already disposed");
			}

			long completedUntil;
			if (iteratorName is null || FasterLog.RecoveredIterators is null)
			{
				completedUntil = FasterLog.CommittedBeginAddress;
			}
			else
			{
				if (!FasterLog.RecoveredIterators.TryGetValue(iteratorName, out completedUntil))
				{
					completedUntil = FasterLog.CommittedBeginAddress;
				}
			}

			return FasterLog.Scan(completedUntil, long.MaxValue, iteratorName, scanUncommitted: true);
		}

		public async IAsyncEnumerable<byte[]> Subscribe(string? iteratorName = default,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("Already disposed");
			}

			using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

			async Task CommitTask(CancellationToken stoppingToken)
			{
				try
				{
					while (!stoppingToken.IsCancellationRequested)
					{
						await Task.Delay(300, stoppingToken).ConfigureAwait(false);
						await FasterLog.CommitAsync(stoppingToken).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException)
				{
					// Ignore
				}
			}

			_ = CommitTask(linkedTokenSource.Token);

			long completedUntil = FasterLog.CommittedBeginAddress;
			while (!cancellationToken.IsCancellationRequested)
			{
				using FasterLogScanIterator iterator = GetIterator(iteratorName);
				await foreach ((byte[]? bytes, int _, long _, long _) in iterator
					.GetAsyncEnumerable(cancellationToken)
					.Catch<(byte[], int, long, long), OperationCanceledException>(_ => AsyncEnumerable.Empty<(byte[], int, long, long)>())
					.ConfigureAwait(false))
				{
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}

					yield return bytes;

					completedUntil = iterator.NextAddress;
					FasterLog.TruncateUntil(completedUntil);
					iterator.CompleteUntil(completedUntil);
				}
			}

			if (_flushOnStop)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				await FasterLog.CommitAsync(CancellationToken.None).ConfigureAwait(false);
				using FasterLogScanIterator flushIterator =
					FasterLog.Scan(completedUntil, FasterLog.CommittedUntilAddress, iteratorName, false);
				await foreach ((byte[] bytes, int _, long currentAddress, long _) in flushIterator
					.GetAsyncEnumerable(CancellationToken.None)
					.ConfigureAwait(false))
				{
					if (bytes is not null)
					{
						yield return bytes;
					}

					FasterLog.TruncateUntil(flushIterator.NextAddress);
					flushIterator.CompleteUntil(flushIterator.NextAddress);
					if (stopwatch.ElapsedMilliseconds > 1000 && _logger.IsEnabled(LogLevel.Warning))
					{
						_logger.LogWarning(
							"Subscription Queue: {FileName}. Reading last entries before closing, it may take a while. Position {CurrentAddress}/{CommittedUntilAddress}",
							_device.FileName, currentAddress, FasterLog.CommittedUntilAddress);
						stopwatch.Restart();
					}
				}
			}
		}
	}
}
