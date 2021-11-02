using System;

using MediatR;

using vivego.Scheduler.Model;
using vivego.Serializer;

namespace vivego.Scheduler
{
	public sealed class ScheduledNotification : IScheduledNotification
	{
		public ScheduledNotification(string id,
			DateTimeOffset triggerAt,
			TimeSpan triggerIn,
			TimeSpan? repeatEvery,
			INotification notification)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
			Id = id;
			TriggerAt = triggerAt;
			Notification = notification ?? throw new ArgumentNullException(nameof(notification));
			TriggerIn = triggerIn;
			RepeatEvery = repeatEvery;
		}

		public ScheduledNotification(ScheduledRequest scheduledRequest, ISerializer serializer)
		{
			if (scheduledRequest is null) throw new ArgumentNullException(nameof(scheduledRequest));
			if (serializer is null) throw new ArgumentNullException(nameof(serializer));
			Id = scheduledRequest.Id;
			TriggerAt = DateTimeOffset.FromUnixTimeMilliseconds(scheduledRequest.TriggerAtUnixTimeInMilliSeconds);
			TriggerIn = TimeSpan.FromMilliseconds(scheduledRequest.TriggerInMilliSeconds);
			RepeatEvery = scheduledRequest.RepeatEveryMilliSeconds <= 0
				? default
				: TimeSpan.FromMilliseconds(scheduledRequest.RepeatEveryMilliSeconds);
			INotification? notification = serializer.Deserialize<INotification>(scheduledRequest.Notification).Result;
			Notification = notification ?? throw new ArgumentNullException(nameof(scheduledRequest.Notification));
		}

		public string Id { get; }
		public DateTimeOffset TriggerAt { get; }
		public TimeSpan TriggerIn { get; }
		public TimeSpan? RepeatEvery { get; }
		public INotification Notification { get; }
	}
}