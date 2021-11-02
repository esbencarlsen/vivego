using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace vivego.Scheduler
{
	public sealed class LoggingSchedulerDispatcher : ISchedulerDispatcher
	{
		private readonly ILogger<LoggingSchedulerDispatcher> _logger;

		public LoggingSchedulerDispatcher(ILogger<LoggingSchedulerDispatcher> logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public Task Dispatch(IScheduledNotification scheduledNotification, CancellationToken cancellationToken)
		{
			if (scheduledNotification is null) throw new ArgumentNullException(nameof(scheduledNotification));
			_logger.LogError("Dispatch Event with ID {NotificationId} Of Type: {NotificationType}",
				scheduledNotification.Id,
				scheduledNotification.Notification.GetType());
			return Task.CompletedTask;
		}
	}
}
