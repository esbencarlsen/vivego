using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

namespace vivego.Scheduler
{
	public sealed class MediatrSchedulerDispatcher : ISchedulerDispatcher
	{
		private readonly IMediator _mediator;

		public MediatrSchedulerDispatcher(IMediator mediator)
		{
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

		public Task Dispatch(IScheduledNotification scheduledNotification, CancellationToken cancellationToken)
		{
			if (scheduledNotification is null) throw new ArgumentNullException(nameof(scheduledNotification));
			return _mediator.Publish(scheduledNotification.Notification, cancellationToken);
		}
	}
}
