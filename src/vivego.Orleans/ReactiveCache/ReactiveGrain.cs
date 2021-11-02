using System;
using System.Threading.Tasks;

using Orleans;

namespace vivego.Orleans.ReactiveCache;

public class ReactiveGrain : Grain
{
	/// <summary>
	/// Registers a simple near-zero period timer that ignores <see cref="TimeoutException"/> exceptions.
	/// Does not make assumptions on how the reactive poll must work otherwise.
	/// </summary>
	protected IDisposable RegisterReactivePoll(Func<object, Task> poll, object? state)
	{
		return RegisterTimer(async _ =>
			{
				try
				{
					await poll(_).ConfigureAwait(true);
				}
				catch (TimeoutException)
				{
				}
			},
			state,
			TimeSpan.Zero,
			TimeSpan.FromTicks(1));
	}

	/// <summary>
	/// Registers a simple near-zero period timer that ignores <see cref="TimeoutException"/> exceptions.
	/// Does make assumptions on how the reactive poll must work.
	/// 1) Calls <paramref name="initialize"/> to resolve the initialization value and then <paramref name="apply"/> to apply it.
	/// 2) Calls the <paramref name="poll"/> action until it times out or it returns a result.
	/// 3) If <paramref name="poll"/> fails with a <see cref="TimeoutException"/> then it ignores it and calls <paramref name="poll"/> again.
	/// 4) When <paramref name="poll"/> returns a value, calls <paramref name="validate"/> on the value.
	/// 5) If the <paramref name="validate"/> returns true, then calls <paramref name="apply"/>, otherwise calls <paramref name="failed"/>.
	/// 6) Goes back to 2).
	/// </summary>
	protected async Task<IDisposable> RegisterReactivePoll<T>(Func<ValueTask<T>>? initialize,
		Func<ValueTask<T>> poll,
		Func<T, bool> validate,
		Func<T, ValueTask> apply,
		Func<T, ValueTask>? failed = default)
	{
		if (poll is null) throw new ArgumentNullException(nameof(poll));
		if (validate is null) throw new ArgumentNullException(nameof(validate));
		if (apply is null) throw new ArgumentNullException(nameof(apply));

		if (initialize is not null)
		{
			T init = await initialize().ConfigureAwait(true);
			await apply(init).ConfigureAwait(true);
		}

		return RegisterTimer(async _ =>
			{
				try
				{
					T update = await poll().ConfigureAwait(true);
					if (validate(update))
					{
						await apply(update).ConfigureAwait(true);
					}
					else
					{
						if (failed is not null)
						{
							await failed(update).ConfigureAwait(true);
						}
					}
				}
				catch (TimeoutException)
				{
				}
			},
			null,
			TimeSpan.Zero,
			TimeSpan.FromTicks(1));
	}

	protected IDisposable RegisterReactivePoll(Func<Task> poll)
	{
		return RegisterReactivePoll(_ => poll(), null);
	}
}
