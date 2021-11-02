using System;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.core.Actors
{
	public static class ActorExtensions
	{
		public static Task<TResult> Run<TState, TResult>(this Actor actor,
			Func<TState, CancellationToken, Task<TResult>> func,
			TState state,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

			static async Task FunctionWrapper((Func<TState, CancellationToken, Task<TResult>> func, TState state, TimeSpan? taskTimeout, CancellationToken cancellationToken, TaskCompletionSource<TResult> taskCompletionSource) valueTuple)
			{
				try
				{
					TResult value;
					if (valueTuple.taskTimeout.HasValue)
					{
						value = await valueTuple.func(valueTuple.state, valueTuple.cancellationToken)
							.WithTimeout(valueTuple.taskTimeout.Value, valueTuple.cancellationToken)
							.ConfigureAwait(false);
					}
					else
					{
						value = await valueTuple.func(valueTuple.state, valueTuple.cancellationToken)
							.WithCancellation(valueTuple.cancellationToken)
							.ConfigureAwait(false);
					}

					valueTuple.taskCompletionSource.TrySetResult(value);
				}
				catch (Exception e)
				{
					valueTuple.taskCompletionSource.TrySetException(e);
				}
			}

			actor.Post((func, state, taskTimeout, cancellationToken, taskCompletionSource), FunctionWrapper);
			return taskCompletionSource.Task;
		}

		public static Task<TResult> Run<TState, TResult>(this Actor actor,
			Func<TState, Task<TResult>> func,
			TState state,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.Run((s, _) => func(s), state, taskTimeout, cancellationToken);
		}

		public static Task<TResult> Run<TResult>(this Actor actor,
			Func<CancellationToken, Task<TResult>> func,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.Run<object?, TResult>((_, token) => func(token), default, taskTimeout, cancellationToken);
		}

		public static Task<TResult> Run<TResult>(this Actor actor,
			Func<Task<TResult>> func,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			return actor.Run<object?, TResult>((_, _) => func(), default, taskTimeout, cancellationToken);
		}

		public static Task Run<T>(this Actor actor,
			Func<T, CancellationToken, Task> func,
			T state,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.Run<T, object?>(async (s, token) =>
			{
				await func(s, token).ConfigureAwait(false);
				return default;
			}, state, taskTimeout, cancellationToken);
		}

		public static Task Run(this Actor actor,
			Func<CancellationToken, Task> func,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.Run<object?, object?>(async (_, token) =>
			{
				await func(token).ConfigureAwait(false);
				return default;
			}, default, taskTimeout, cancellationToken);
		}

		public static Task Run<T>(this Actor actor,
			Func<T, Task> func,
			T state,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.Run<T, object?>(async (s, _) =>
			{
				await func(s).ConfigureAwait(false);
				return default;
			}, state, taskTimeout, cancellationToken);
		}

		public static Task Run(this Actor actor,
			Func<Task> func,
			TimeSpan? taskTimeout = null,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.Run<object?, object?>(async (_, _) =>
			{
				await func().ConfigureAwait(false);
				return default;
			}, default, taskTimeout, cancellationToken);
		}

		public static Task<TResult> RunFunc<TState, TResult>(this Actor actor,
			Func<TState, CancellationToken, TResult> func,
			TState state,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
			static void FunctionWrapper((Func<TState, CancellationToken, TResult> func, TState state, CancellationToken cancellationToken, TaskCompletionSource<TResult> taskCompletionSource) valueTuple)
			{
				try
				{
					TResult value = valueTuple.func(valueTuple.state, valueTuple.cancellationToken);
					valueTuple.taskCompletionSource.TrySetResult(value);
				}
				catch (Exception e)
				{
					valueTuple.taskCompletionSource.TrySetException(e);
				}
			}

			actor.Post((func, state, cancellationToken, taskCompletionSource), FunctionWrapper);
			return taskCompletionSource.Task;
		}

		public static Task<TResult> RunFunc<TResult>(this Actor actor,
			Func<CancellationToken, TResult> func,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc<object?, TResult>((_, token) => func(token), default, cancellationToken);
		}

		public static Task<TResult> RunFunc<TState, TResult>(this Actor actor,
			Func<TState, TResult> func,
			TState state,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc((s, _) => func(s), state, cancellationToken);
		}

		public static Task<TResult> RunFunc<TResult>(this Actor actor,
			Func<TResult> func,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc<object?, TResult>((_, _) => func(), default, cancellationToken);
		}

		public static Task RunFunc<TState>(this Actor actor,
			Action<TState, CancellationToken> func,
			TState state,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc<TState, object?>((s, token) =>
			{
				func(s, token);
				return default;
			}, state, cancellationToken);
		}

		public static Task RunFunc(this Actor actor,
			Action<CancellationToken> func,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc<object?, object?>((_, token) =>
			{
				func(token);
				return default;
			}, default, cancellationToken);
		}

		public static Task RunFunc<TState>(this Actor actor,
			Action<TState> func,
			TState state,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc<TState, object?>((s, _) =>
			{
				func(s);
				return default;
			}, state, cancellationToken);
		}

		public static Task RunFunc<TState>(this Actor actor,
			Action func,
			TState state,
			CancellationToken cancellationToken = default)
		{
			if (actor is null) throw new ArgumentNullException(nameof(actor));
			if (func is null) throw new ArgumentNullException(nameof(func));
			return actor.RunFunc<TState, object?>((_, _) =>
			{
				func();
				return default;
			}, state, cancellationToken);
		}
	}
}
