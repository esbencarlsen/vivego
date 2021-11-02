using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using MediatR;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using vivego.Scheduler.Cancel;
using vivego.Scheduler.Get;
using vivego.Scheduler.GetAll;
using vivego.Scheduler.Schedule;

namespace vivego.Scheduler
{
	public sealed class DefaultScheduler : BackgroundService, IScheduler
	{
		private readonly IMediator _mediator;
		private readonly ISchedulerDispatcher _schedulerDispatcher;
		private readonly ILogger<DefaultScheduler> _logger;
		private readonly IOptions<DefaultSchedulerOptions> _options;

		public DefaultScheduler(
			string name,
			IMediator mediator,
			ISchedulerDispatcher schedulerDispatcher,
			ILogger<DefaultScheduler> logger,
			IOptions<DefaultSchedulerOptions> options)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
			_schedulerDispatcher = schedulerDispatcher ?? throw new ArgumentNullException(nameof(schedulerDispatcher));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public string Name { get; }

		public Task Schedule(string id,
			INotification notification,
			TimeSpan triggerIn,
			TimeSpan? repeatEvery = default,
			CancellationToken cancellationToken = default) =>
			_mediator.Send(new ScheduleRequest(id, notification, triggerIn, repeatEvery), cancellationToken);

		public Task Cancel(string id, CancellationToken cancellationToken = default) =>
			_mediator.Send(new CancelScheduledRequest(id), cancellationToken);

		public Task<IScheduledNotification?> Get(string id, CancellationToken cancellationToken = default) => _mediator.Send(new GetScheduledRequest(id), cancellationToken);

		public async IAsyncEnumerable<IScheduledNotification> GetAll(
			DateTimeOffset from,
			DateTimeOffset to,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			IAsyncEnumerable<IScheduledNotification> asyncEnumerable = await _mediator
				.Send(new GetAllScheduledRequests(from, to), cancellationToken)
				.ConfigureAwait(false);
			await foreach (IScheduledNotification scheduledNotification in asyncEnumerable
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false))
			{
				yield return scheduledNotification;
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					ActionBlock<IScheduledNotification> actionBlock = new(
						async scheduledNotification =>
						{
							try
							{
								if (scheduledNotification.RepeatEvery.HasValue && scheduledNotification.RepeatEvery.Value > TimeSpan.Zero)
								{
									await Schedule(scheduledNotification.Id,
											scheduledNotification.Notification,
											scheduledNotification.RepeatEvery.Value,
											scheduledNotification.RepeatEvery.Value,
											stoppingToken)
										.ConfigureAwait(false);
								}
								else
								{
									await Cancel(scheduledNotification.Id, stoppingToken).ConfigureAwait(false);
								}

								await _schedulerDispatcher.Dispatch(scheduledNotification, stoppingToken).ConfigureAwait(false);
							}
							catch (Exception e)
							{
								_logger.LogError(e, "Error while processing scheduler entry: {NotificationId}", scheduledNotification.Id);
							}
						}, new ExecutionDataflowBlockOptions
						{
							CancellationToken = stoppingToken,
							EnsureOrdered = false,
							MaxDegreeOfParallelism = _options.Value.DegreeOfParallelism,
							SingleProducerConstrained = true
						});

					await foreach (IScheduledNotification notification in GetAll(DateTimeOffset.MinValue, DateTimeOffset.UtcNow, stoppingToken).ConfigureAwait(false))
					{
						actionBlock.Post(notification);
					}

					actionBlock.Complete();
					await actionBlock.Completion.ConfigureAwait(false);
					await Task.Delay(_options.Value.CheckInterval, stoppingToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Error while polling / executing scheduler queue: {Name}", Name);
				}
			}
		}
	}
}
