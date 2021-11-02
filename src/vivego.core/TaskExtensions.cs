using System;
#if !NET6_0
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace vivego.core;

public static class TaskExtensions
{
	private static readonly Action<Task> s_ignoreTaskContinuation = t => { _ = t.Exception; };

	public static readonly Task<bool> True = Task.FromResult(true);
	public static readonly Task<bool> False = Task.FromResult(false);

	/// <summary>
	/// Wraps a task with one that will complete as cancelled based on a cancellation token,
	/// allowing someone to await a task but be able to break out early by cancelling the token.
	/// </summary>
	/// <typeparam name="T">The type of value returned by the task.</typeparam>
	/// <param name="task">The task to wrap.</param>
	/// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
	/// <returns>The wrapping task.</returns>
	public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
	{
#if NET6_0
		ArgumentNullException.ThrowIfNull(task);
		return task.WaitAsync(cancellationToken);
#else
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (!cancellationToken.CanBeCanceled || task.IsCompleted)
		{
			return task;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<T>(cancellationToken);
		}

		return WithCancellationSlow(task, cancellationToken);
#endif
	}

	/// <summary>
	/// Wraps a task with one that will complete as cancelled based on a cancellation token,
	/// allowing someone to await a task but be able to break out early by cancelling the token.
	/// </summary>
	/// <param name="task">The task to wrap.</param>
	/// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
	/// <returns>The wrapping task.</returns>
	public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
	{
#if NET6_0
		ArgumentNullException.ThrowIfNull(task);
		return task.WaitAsync(cancellationToken);
#else
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (!cancellationToken.CanBeCanceled || task.IsCompleted)
		{
			return task;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}

		return WithCancellationSlow(task, false, cancellationToken);
#endif
	}

#if NET6_0
#else
	/// <summary>
	/// Wraps a task with one that will complete as cancelled based on a cancellation token,
	/// allowing someone to await a task but be able to break out early by cancelling the token.
	/// </summary>
	/// <param name="task">The task to wrap.</param>
	/// <param name="continueOnCapturedContext">A value indicating whether *internal* continuations required to respond to cancellation should run on the current <see cref="SynchronizationContext"/>.</param>
	/// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
	/// <returns>The wrapping task.</returns>
	internal static Task WithCancellation(this Task task, bool continueOnCapturedContext, CancellationToken cancellationToken)
	{
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (!cancellationToken.CanBeCanceled || task.IsCompleted)
		{
			return task;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}

		return WithCancellationSlow(task, continueOnCapturedContext, cancellationToken);
	}

	/// <summary>
	/// Wraps a task with one that will complete as cancelled based on a cancellation token,
	/// allowing someone to await a task but be able to break out early by cancelling the token.
	/// </summary>
	/// <typeparam name="T">The type of value returned by the task.</typeparam>
	/// <param name="task">The task to wrap.</param>
	/// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
	/// <returns>The wrapping task.</returns>
	private static async Task<T> WithCancellationSlow<T>(Task<T> task, CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		await using ConfiguredAsyncDisposable _ = cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs).ConfigureAwait(false);
		if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
		{
			cancellationToken.ThrowIfCancellationRequested();
		}

		// Rethrow any fault/cancellation exception, even if we awaited above.
		// But if we skipped the above if branch, this will actually yield
		// on an incomplete task.
		return await task.ConfigureAwait(false);
	}

	/// <summary>
	/// Wraps a task with one that will complete as cancelled based on a cancellation token,
	/// allowing someone to await a task but be able to break out early by cancelling the token.
	/// </summary>
	/// <param name="task">The task to wrap.</param>
	/// <param name="continueOnCapturedContext">A value indicating whether *internal* continuations required to respond to cancellation should run on the current <see cref="SynchronizationContext"/>.</param>
	/// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
	/// <returns>The wrapping task.</returns>
	private static async Task WithCancellationSlow(this Task task, bool continueOnCapturedContext, CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		await using ConfiguredAsyncDisposable _ = cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs).ConfigureAwait(false);
		if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(continueOnCapturedContext))
		{
			cancellationToken.ThrowIfCancellationRequested();
		}

		// Rethrow any fault/cancellation exception, even if we awaited above.
		// But if we skipped the above if branch, this will actually yield
		// on an incomplete task.
		await task.ConfigureAwait(continueOnCapturedContext);
	}
#endif

	/// <summary>
	/// Wraps a task with one that will complete as cancelled based on a cancellation token,
	/// allowing someone to await a task but be able to break out early by cancelling the token.
	/// </summary>
	/// <param name="task">The task to wrap.</param>
	/// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
	/// <returns>The wrapping task.</returns>
	public static async Task WithSilentCancellation(this Task task, CancellationToken cancellationToken)
	{
#if NET6_0
		ArgumentNullException.ThrowIfNull(task);
		try
		{
			await task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// Ignore
		}
#else
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (!cancellationToken.CanBeCanceled || task.IsCompleted)
		{
			return;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		await using ConfiguredAsyncDisposable _ = cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs).ConfigureAwait(false);
		await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
#endif
	}

	public static async ValueTask<T> WithDefaultOnTimeout<T>(this Task<T> task,
		TimeSpan timeout,
		T defaultValue,
		CancellationToken cancellationToken = default)
	{
#if NET6_0
		ArgumentNullException.ThrowIfNull(task);
		try
		{
			return await task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return defaultValue;
		}
		catch (TimeoutException)
		{
			return defaultValue;
		}
#else
		if (task is null) throw new ArgumentNullException(nameof(task));
		using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellationTokenSource.CancelAfter(timeout);
		try
		{
			return await task
				.WithCancellation(cancellationTokenSource.Token)
				.ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return defaultValue;
		}
#endif
	}

	public static async Task<T> WithTimeout<T>(this Task<T> task,
		TimeSpan timeout,
		CancellationToken cancellationToken = default)
	{
#if NET6_0
		ArgumentNullException.ThrowIfNull(task);
		try
		{
			return await task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
		}
		catch (TimeoutException)
		{
			throw new OperationCanceledException();
		}
#else
		if (task is null) throw new ArgumentNullException(nameof(task));
		using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellationTokenSource.CancelAfter(timeout);
		return await task
			.WithCancellation(cancellationTokenSource.Token)
			.ConfigureAwait(false);
#endif
	}

	public static async Task WithTimeout(this Task task,
		TimeSpan timeout,
		bool continueOnCapturedContext = true,
		CancellationToken cancellationToken = default)
	{
#if NET6_0
		ArgumentNullException.ThrowIfNull(task);
		try
		{
			await task.WaitAsync(timeout, cancellationToken).ConfigureAwait(continueOnCapturedContext);
		}
		catch (TimeoutException)
		{
			throw new OperationCanceledException();
		}
#else
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (task.IsCompleted)
		{
			return;
		}

		using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellationTokenSource.CancelAfter(timeout);
		await task
			.WithCancellation(cancellationTokenSource.Token)
			.ConfigureAwait(continueOnCapturedContext);
#endif
	}

#pragma warning disable CA1030 // Use events where appropriate
	public static void FireAndForget(this Task task)
#pragma warning restore CA1030 // Use events where appropriate
	{
		if (task is null)
		{
			throw new ArgumentNullException(nameof(task));
		}

		// Fire and forget
		if (task.IsCompleted)
		{
			_ = task.Exception;
		}
		else
		{
			_ = task.ContinueWith(
				s_ignoreTaskContinuation,
				CancellationToken.None,
				TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}
	}

	public static Task<T> CanceledTask<T>()
	{
		TaskCompletionSource<T> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		tcs.SetCanceled();
		return tcs.Task;
	}

	public static Task<T> NeverTask<T>()
	{
		return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously).Task;
	}

	public static Task<T> ToTask<T>(this T result)
	{
		return Task.FromResult(result);
	}
}
