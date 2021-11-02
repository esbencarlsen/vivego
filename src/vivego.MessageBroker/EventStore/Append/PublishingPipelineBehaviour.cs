using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Hosting;

using vivego.MessageBroker.PublishSubscribe;

namespace vivego.MessageBroker.EventStore.Append;

public sealed class PublishingPipelineBehaviour : BackgroundService,
	IPipelineBehavior<AppendRequest, long>
{
	private readonly IPublishSubscribe _publishSubscribe;
	private readonly Channel<(string Topic, long EventId)> _triggerChannel = Channel.CreateUnbounded<(string, long)>();

	public PublishingPipelineBehaviour(IPublishSubscribe publishSubscribe)
	{
		_publishSubscribe = publishSubscribe ?? throw new ArgumentNullException(nameof(publishSubscribe));
	}

	public async Task<long> Handle(AppendRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<long> next)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(next);

		long eventId = await next().ConfigureAwait(false);
		_triggerChannel.Writer.TryWrite((request.Topic, eventId));

		return eventId;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await _triggerChannel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false);

				Dictionary<string, long> dictionary = new();
				while (_triggerChannel.Reader.TryRead(out (string Topic, long EventId) tuple)
					&& dictionary.Count < 10000)
				{
					// Only add lowest EventId In batch for each topic
					dictionary.TryAdd(tuple.Topic, tuple.EventId);
				}

				await Parallel
					.ForEachAsync(dictionary, stoppingToken, (tuple, _) => _publishSubscribe.Publish(tuple.Key, BitConverter.GetBytes(tuple.Value)))
					.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Ignore
			}
		}
	}
}
