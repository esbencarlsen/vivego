using System;
using System.Collections.Concurrent;
using System.Threading;

namespace vivego.core
{
	public abstract class DisposableBase : IDisposable
	{
		private readonly ConcurrentStack<Action> _disposeTasks = new();

		private readonly Lazy<CancellationTokenSource> _lazyCancellationTokenSource = new(() => new CancellationTokenSource(), true);

		private long _disposeSignaled;

		protected CancellationToken CancellationToken => _lazyCancellationTokenSource.Value.Token;

		public bool IsDisposed => Interlocked.Read(ref _disposeSignaled) != 0;

		protected void RegisterDisposable(Action action)
		{
			if (action is null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			_disposeTasks.Push(action);
		}

		protected void RegisterDisposable(IDisposable disposable)
		{
			if (disposable is null)
			{
				throw new ArgumentNullException(nameof(disposable));
			}

			RegisterDisposable(disposable.Dispose);
		}

		/// <summary>
		///     Do cleanup here
		/// </summary>
		protected virtual void Cleanup()
		{
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				bool hasDisposed = Interlocked.CompareExchange(ref _disposeSignaled, 1, 0) == 1;
				if (hasDisposed)
				{
					return;
				}

				Cleanup();

				while (_disposeTasks.TryPop(out Action? disposableAction))
				{
					try
					{
						disposableAction();
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}

				if (_lazyCancellationTokenSource.IsValueCreated)
				{
					using CancellationTokenSource cancellationTokenSource = _lazyCancellationTokenSource.Value;
					cancellationTokenSource.Cancel(false);
				}
			}
		}

		public void Dispose()
		{
			// Dispose of unmanaged resources.
			Dispose(true);
			// Take yourself off the finalization queue
			// to prevent finalization from executing a second time.
			GC.SuppressFinalize(this);
		}
	}

	public sealed class EmptyDisposable : DisposableBase
	{
	}
}
