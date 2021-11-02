using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Concurrency;
using Orleans.Configuration;
using Orleans.Placement;

using vivego.core;

namespace vivego.Orleans.ReactiveCache;

[Reentrant]
[PreferLocalPlacement]
public sealed class ProducerGrain<T> : Grain, IProducerGrain<T>
{
	private readonly TimeSpan _waitTimeout;

	private VersionedValue<T> _state = VersionedValue<T>.None.NextVersion(default);
	private TaskCompletionSource<VersionedValue<T>> _wait = new();

	public ProducerGrain(IOptions<SiloMessagingOptions> messagingOptions)
	{
		if (messagingOptions is null) throw new ArgumentNullException(nameof(messagingOptions));

		// this timeout helps resolve long polls gracefully just before orleans breaks them with a timeout exception
		// while not necessary for the reactive caching pattern to work
		// it avoid polluting the network and the logs with stack traces from timeout exceptions
		_waitTimeout = messagingOptions.Value.ResponseTimeout.Subtract(TimeSpan.FromSeconds(2));
	}

	public override Task OnDeactivateAsync()
	{
		_wait.SetResult(VersionedValue<T>.None);
		return base.OnDeactivateAsync();
	}

	public ValueTask Set(T value)
	{
		// update the state
		_state = _state.NextVersion(value);

		// fulfill waiting promises
		_wait.SetResult(_state);
		_wait = new TaskCompletionSource<VersionedValue<T>>();

		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Returns the current state without polling.
	/// </summary>
	public ValueTask<VersionedValue<T>> Get() => ValueTask.FromResult(_state);

	/// <summary>
	/// If the given version is the same as the current version then resolves when a new version of data is available.
	/// If no new data become available within the orleans response timeout minus some margin, then resolves with a "no data" response.
	/// Otherwise returns the current data immediately.
	/// </summary>
	public ValueTask<VersionedValue<T>> LongPoll(VersionToken knownVersion) =>
		knownVersion == _state.Version
			? _wait.Task.WithDefaultOnTimeout(_waitTimeout, VersionedValue<T>.None)
			: ValueTask.FromResult(_state);
}
