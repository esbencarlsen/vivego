using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;

using vivego.core;

namespace vivego.MessageBroker.PublishSubscribe;

public sealed class DefaultPublishSubscribe : IPublishSubscribe,
	INotificationGrainObserver,
	ISiloStatusListener
{
	private readonly IClusterClient _clusterClient;
	private readonly ILogger<DefaultPublishSubscribe> _logger;
	private readonly Dictionary<string, AsyncQueue<byte[]>> _channels = new();
	private readonly Lazy<Task<INotificationGrainObserver>> _grainObserver;
	private TaskCompletionSource<object> _completionSource = new();

	public DefaultPublishSubscribe(
		IClusterClient clusterClient,
		ILogger<DefaultPublishSubscribe> logger,
		ISiloStatusOracle? siloStatusOracle = default)
	{
		_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_grainObserver = new Lazy<Task<INotificationGrainObserver>>(() => _clusterClient.CreateObjectReference<INotificationGrainObserver>(this), true);

		siloStatusOracle?.SubscribeToSiloStatusEvents(this);
	}

	public ValueTask Publish(string topic, byte[] data)
	{
		return _clusterClient
			.GetGrain<IPubSubGrain>(topic)
			.Publish(data);
	}

	public async IAsyncEnumerable<byte[]> Subscribe(string topic,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		Task subscribeTask;
		using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
		{
			INotificationGrainObserver grainObserver = await _grainObserver.Value.ConfigureAwait(false);
			await _clusterClient
				.GetGrain<IPubSubGrain>(topic)
				.Subscribe(grainObserver)
				.ConfigureAwait(false);

			subscribeTask = UpdateSubscriptionTask(topic, cancellationTokenSource.Token);

			AsyncQueue<byte[]>? queue;
			lock (_channels)
			{
				if (!_channels.TryGetValue(topic, out queue))
				{
					queue = new AsyncQueue<byte[]>();
					_channels[topic] = queue;
				}
			}

			try
			{
				await foreach (byte[] bytes in queue
					.WithCancellation(cancellationToken)
					.ConfigureAwait(false))
				{
					yield return bytes;
				}
			}
			finally
			{
				lock (_channels)
				{
					if (!queue.HasSubscribers)
					{
						_channels.Remove(topic);
					}
				}
			}
		}

		await subscribeTask.ConfigureAwait(false);
	}

	private async Task UpdateSubscriptionTask(string topic, CancellationToken cancellationToken)
	{
		INotificationGrainObserver grainObserver = await _grainObserver.Value.ConfigureAwait(false);
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await Task
					.WhenAny(
						_completionSource.Task,
						Task.Delay(TimeSpan.FromSeconds(15), cancellationToken))
					.ConfigureAwait(false);

				await _clusterClient
					.GetGrain<IPubSubGrain>(topic)
					.Subscribe(grainObserver)
					.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Ok, ignore
			}
#pragma warning disable CA1031
			catch (Exception e)
			{
				_logger.LogError(e, "While subscribing, grain observer, retrying forever");
			}
		}
	}

	public void Notify(string topic, byte[] data)
	{
		lock (_channels)
		{
			if (_channels.TryGetValue(topic, out AsyncQueue<byte[]>? queue))
			{
				queue.TryWrite(data);
			}
		}
	}

	public void SiloStatusChangeNotification(SiloAddress updatedSilo, SiloStatus status)
	{
		TaskCompletionSource<object> previousCompletionSource = _completionSource;
		_completionSource = new TaskCompletionSource<object>();
		previousCompletionSource.TrySetResult(default!);
	}
}
