using System;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;

namespace vivego.Orleans.ReactiveCache;

[Reentrant]
[StatelessWorker]
public class ProducerCacheGrain<T> : ReactiveGrain, IProducerCacheGrain<T> where T : notnull
{
	private VersionedValue<T> _cache = VersionedValue<T>.None;
	private IDisposable? _poll;

	private string GrainKey => this.GetPrimaryKeyString();

	public override async Task OnActivateAsync()
	{
		// start long polling
		_poll = await RegisterReactivePoll(
				() => GrainFactory.GetGrain<IProducerGrain<T>>(GrainKey).Get(),
				() => GrainFactory.GetGrain<IProducerGrain<T>>(GrainKey).LongPoll(_cache.Version),
				result => result.IsValid,
				apply =>
				{
					_cache = apply;
					return ValueTask.CompletedTask;
				})
			.ConfigureAwait(true);

		await base.OnActivateAsync().ConfigureAwait(true);
	}

	public override Task OnDeactivateAsync()
	{
		_poll?.Dispose();
		return base.OnDeactivateAsync();
	}

	public ValueTask<T?> Get() => ValueTask.FromResult(_cache.Value);
}
