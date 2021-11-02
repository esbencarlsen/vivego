using System;

using MediatR;

namespace vivego.Scheduler.Schedule
{
	public sealed record ScheduleRequest : IRequest
	{
		public string Id { get; }
		public INotification Notification { get; }
		public DateTimeOffset TriggerAt { get; }
		public TimeSpan TriggerIn { get; }
		public TimeSpan? RepeatEvery { get; }

		public ScheduleRequest(string id, INotification notification, TimeSpan triggerIn, TimeSpan? repeatEvery = default)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
			if (triggerIn < TimeSpan.Zero) throw new ArgumentException("Value cannot be less or equal to zero.", nameof(triggerIn));
			if (repeatEvery.HasValue && repeatEvery.Value <= TimeSpan.Zero) throw new ArgumentException("Value cannot be less or equal to zero.", nameof(repeatEvery));

			Id = id;
			Notification = notification;
			TriggerAt = DateTimeOffset.UtcNow.Add(triggerIn);
			TriggerIn = triggerIn;
			RepeatEvery = repeatEvery;
		}
	}
}