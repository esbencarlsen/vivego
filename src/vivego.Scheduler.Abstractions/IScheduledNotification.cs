using System;

using MediatR;

namespace vivego.Scheduler
{
	public interface IScheduledNotification : INotification
	{
		string Id { get; }
		DateTimeOffset TriggerAt { get; }
		TimeSpan TriggerIn { get; }
		TimeSpan? RepeatEvery { get; }
		INotification Notification { get; }
	}
}