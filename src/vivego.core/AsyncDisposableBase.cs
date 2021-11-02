using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.core
{
	public abstract class AsyncDisposableBase : IAsyncDisposable
	{
		private readonly ConcurrentStack<Func<Task>> _disposeTasks = new();

		private readonly Lazy<CancellationTokenSource> _lazyCancellationTokenSource = new(() => new CancellationTokenSource(), true);

		private long _disposeSignaled;

		protected CancellationToken CancellationToken => _lazyCancellationTokenSource.Value.Token;

		public bool IsDisposed => Interlocked.Read(ref _disposeSignaled) != 0;

		public async ValueTask DisposeAsync()
		{
			bool hasDisposed = Interlocked.CompareExchange(ref _disposeSignaled, 1, 0) == 1;
			if (hasDisposed)
			{
				return;
			}

			await Cleanup().ConfigureAwait(false);

			while (_disposeTasks.TryPop(out Func<Task>? disposable))
			{
				Task shutdownTask = disposable();
				if (shutdownTask is not null)
				{
					try
					{
						await shutdownTask.ConfigureAwait(false);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}

			if (_lazyCancellationTokenSource.IsValueCreated)
			{
				using CancellationTokenSource cancellationTokenSource = _lazyCancellationTokenSource.Value;
				cancellationTokenSource.Cancel(false);
			}

			// Take yourself off the finalization queue
			// to prevent finalization from executing a second time.
			GC.SuppressFinalize(this);
		}

		protected void RegisterDisposable(Func<Task> disposeTask)
		{
			_disposeTasks.Push(async () => await disposeTask().ConfigureAwait(false));
		}

		protected void RegisterDisposable(Action action)
		{
			if (action is null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			RegisterDisposable(() =>
			{
				action();
				return Task.CompletedTask;
			});
		}

		protected void RegisterDisposable(IDisposable disposable)
		{
			if (disposable is null)
			{
				throw new ArgumentNullException(nameof(disposable));
			}

			RegisterDisposable(() =>
			{
				disposable.Dispose();
				return Task.CompletedTask;
			});
		}

		protected void RegisterDisposable(IAsyncDisposable disposable)
		{
			if (disposable is null)
			{
				throw new ArgumentNullException(nameof(disposable));
			}

			RegisterDisposable(() => disposable.DisposeAsync().AsTask());
		}

		/// <summary>
		///     Do cleanup here
		/// </summary>
		protected virtual Task Cleanup()
		{
			return Task.CompletedTask;
		}
	}
}
