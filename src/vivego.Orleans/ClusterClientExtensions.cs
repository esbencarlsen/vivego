using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Streams;

using vivego.core;

namespace vivego.Orleans
{
	public static class ClusterClientExtensions
	{
		public static IAsyncEnumerable<T> Stream<T>(this IClusterClient clusterClient,
			Guid streamId,
			string streamNamespace,
			string streamProviderName,
			ILogger? logger = default,
			CancellationToken cancellationToken = default) where T : class
		{
			if (clusterClient is null)
			{
				throw new ArgumentNullException(nameof(clusterClient));
			}

			if (string.IsNullOrEmpty(streamNamespace))
			{
				throw new ArgumentNullException(nameof(streamNamespace));
			}

			if (string.IsNullOrEmpty(streamProviderName))
			{
				throw new ArgumentNullException(nameof(streamProviderName));
			}

			return Stream().Retry();

			async IAsyncEnumerable<T> Stream()
			{
				CancellationToken grainFactoryExtensionsLifecycleParticipantCancellationToken;
				if (clusterClient.ServiceProvider.GetService(typeof(IOrleansLifecycleParticipantMonitor)) is IOrleansLifecycleParticipantMonitor grainFactoryExtensionsLifecycleParticipant)
				{
					grainFactoryExtensionsLifecycleParticipantCancellationToken = grainFactoryExtensionsLifecycleParticipant.StoppingToken;
				}
				else
				{
					grainFactoryExtensionsLifecycleParticipantCancellationToken = CancellationToken.None;
				}

				using CancellationTokenSource cancellationTokenSource = CancellationTokenSource
					.CreateLinkedTokenSource(cancellationToken, grainFactoryExtensionsLifecycleParticipantCancellationToken);
				CancellationToken linkedToken = cancellationTokenSource.Token;
				IStreamProvider streamProvider = clusterClient.GetStreamProvider(streamProviderName);
				IAsyncStream<T> asyncStream = streamProvider.GetStream<T>(streamId, streamNamespace);
				StreamSequenceToken? streamSequenceToken = default;
				while (!cancellationToken.IsCancellationRequested)
				{
					Channel<T> channel = Channel.CreateUnbounded<T>();
					StreamSubscriptionHandle<T> subscriptionHandle = await asyncStream
						.SubscribeAsync(
							async (value, token) =>
							{
								streamSequenceToken = token;
								await channel.Writer.WriteAsync(value, linkedToken).ConfigureAwait(false);
							},
							exception =>
							{
								logger?.LogError(exception, "Error while streaming");
								return Task.CompletedTask;
							},
							() =>
							{
								channel.Writer.TryComplete();
								return Task.CompletedTask;
							},
							streamSequenceToken)
						.ConfigureAwait(false);

					try
					{
						await foreach (T value in channel.ToAsyncEnumerable(linkedToken).ConfigureAwait(false))
						{
							yield return value;
						}
					}
					finally
					{
						await subscriptionHandle.UnsubscribeAsync().ConfigureAwait(false);
					}
				}
			}
		}
	}
}
