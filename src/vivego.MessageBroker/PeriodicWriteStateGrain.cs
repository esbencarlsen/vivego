using System;
using System.Threading.Tasks;

using Orleans;

namespace vivego.MessageBroker;

public abstract class PeriodicWriteStateGrain<T> : Grain<T> where T : new()
{
	private readonly TimeSpan _maxDirtyTime = TimeSpan.FromMinutes(1); // Default
	private IDisposable? _timerRegistration;
	private bool _clear;
	private bool _dirty;

	public override async Task OnDeactivateAsync()
	{
		await ForceWriteState().ConfigureAwait(true);
		await base.OnDeactivateAsync().ConfigureAwait(true);
	}

	protected override Task WriteStateAsync()
	{
		_clear = false;
		_dirty = true;
		_timerRegistration ??= RegisterTimer(_ => ForceWriteState(), null, _maxDirtyTime, TimeSpan.FromMilliseconds(-1));
		return Task.CompletedTask;
	}

	protected override Task ClearStateAsync()
	{
		State = new T();
		_clear = true;
		_dirty = false;
		_timerRegistration ??= RegisterTimer(_ => ForceWriteState(), null, _maxDirtyTime, TimeSpan.FromMilliseconds(-1));
		return Task.CompletedTask;
	}

	protected Task ForceWriteState()
	{
		_timerRegistration?.Dispose();
		_timerRegistration = null;

		if (_clear)
		{
			_clear = false;
			return base.ClearStateAsync();
		}

		if (_dirty)
		{
			_dirty = false;
			return base.WriteStateAsync();
		}

		return Task.CompletedTask;
	}
}
