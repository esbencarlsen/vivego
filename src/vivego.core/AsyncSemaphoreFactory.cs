using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace vivego.core;

#if NETSTANDARD2_1
public sealed class AsyncSemaphoreRecord
{
	public AsyncSemaphore AsyncSemaphore { get; }
	public Stopwatch LastUpdated { get; }

	public AsyncSemaphoreRecord(AsyncSemaphore asyncSemaphore, Stopwatch lastUpdated)
	{
		AsyncSemaphore = asyncSemaphore;
		LastUpdated = lastUpdated;
	}
}
#else
public sealed record AsyncSemaphoreRecord(AsyncSemaphore AsyncSemaphore, Stopwatch LastUpdated);
#endif

public sealed class AsyncSemaphoreFactory : BackgroundService
{
	private readonly ILogger<AsyncSemaphoreFactory> _logger;
	private readonly ConcurrentDictionary<string, AsyncSemaphoreRecord> _lockDictionary = new(StringComparer.Ordinal);

	public AsyncSemaphoreFactory(ILogger<AsyncSemaphoreFactory> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public AsyncSemaphore Get(string key, int maxConcurrency = 1)
	{
		while (true)
		{
			if (_lockDictionary.TryGetValue(key, out AsyncSemaphoreRecord? asyncSemaphoreRecord))
			{
				lock (asyncSemaphoreRecord.AsyncSemaphore)
				{
					asyncSemaphoreRecord.LastUpdated.Restart();
				}

				return asyncSemaphoreRecord.AsyncSemaphore;
			}

#pragma warning disable CA2000
			asyncSemaphoreRecord = new AsyncSemaphoreRecord(new AsyncSemaphore(_logger, maxConcurrency), Stopwatch.StartNew());
			if (_lockDictionary.TryAdd(key, asyncSemaphoreRecord))
			{
				return asyncSemaphoreRecord.AsyncSemaphore;
			}
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
#if NETSTANDARD2_1
		await Task.CompletedTask.ConfigureAwait(false);
#else
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
				foreach (var (key, (asyncSemaphore, lastUpdated)) in _lockDictionary.ToArray())
				{
					if (lastUpdated.Elapsed > TimeSpan.FromMinutes(1)
						&& !asyncSemaphore.InUse
						&& _lockDictionary.TryRemove(key, out AsyncSemaphoreRecord? asyncSemaphoreRecord))
					{
						await asyncSemaphoreRecord.AsyncSemaphore.DisposeAsync().ConfigureAwait(false);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Ignore
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Error while cleaning");
				await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
			}
		}
#endif
	}
}
