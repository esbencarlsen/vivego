using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace vivego.core;

public sealed class AsyncSemaphore : AsyncDisposableBase
{
	private readonly ILogger _logger;
	private readonly int _maxConcurrency;
	private readonly SemaphoreSlim _semaphore;

	public AsyncSemaphore(
		ILogger logger,
		int maxConcurrency)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_maxConcurrency = maxConcurrency;
		_semaphore = new SemaphoreSlim(
			maxConcurrency,
			maxConcurrency
		);
		RegisterDisposable(_semaphore);
	}

	public bool InUse => _semaphore.CurrentCount != _maxConcurrency;

	public async Task<T> WaitAsync<T>(Func<Task<T>> producer, CancellationToken cancellationToken = default)
	{
		if (producer is null) throw new ArgumentNullException(nameof(producer));

		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			Task<T> task = producer();
			T result = await task.ConfigureAwait(false);
			return result;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task<T> WaitAsync<T>(Func<T> producer, CancellationToken cancellationToken = default)
	{
		if (producer is null) throw new ArgumentNullException(nameof(producer));

		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			return producer();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task WaitAsync(Func<Task> producer, CancellationToken cancellationToken = default)
	{
		if (producer is null) throw new ArgumentNullException(nameof(producer));

		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			Task task = producer();
			await task.ConfigureAwait(false);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public void Wait(Func<Task> producer, CancellationToken cancellationToken = default)
	{
		// block caller
		_semaphore.Wait(cancellationToken);

		_ = Run(async () =>
		{
			try
			{
				Task task = producer();
				await task.ConfigureAwait(false);
			}
			finally
			{
				// release once the async flow is done
				_semaphore.Release();
			}
		}, cancellationToken);
	}

	private async Task Run(Func<Task> body, CancellationToken cancellationToken = default, [CallerMemberName] string name = "")
	{
		try
		{
			await Task.Run(body, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception x)
		{
			_logger.LogError(x, "Unhandled exception in async job {Job}", name);
		}
	}
}
