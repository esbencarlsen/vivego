using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using vivego.Collection.TimeSeries;
using vivego.Scheduler.Model;
using vivego.Serializer;
using vivego.Serializer.Model;

namespace vivego.Scheduler.Schedule
{
	public sealed class ScheduleRequestHandler : IRequestHandler<ScheduleRequest>
	{
		private readonly string _name;
		private readonly ITimeSeries _timeSeries;
		private readonly ISerializer _serializer;

		public ScheduleRequestHandler(string name, ITimeSeries timeSeries, ISerializer serializer)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_timeSeries = timeSeries ?? throw new ArgumentNullException(nameof(timeSeries));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public async Task<Unit> Handle(ScheduleRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (string.IsNullOrEmpty(request.Id)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Id));

			SerializedValue serializedNotification = await _serializer
				.Serialize(request.Notification, cancellationToken)
				.ConfigureAwait(false);
			DateTimeOffset now = DateTimeOffset.UtcNow;
			DateTimeOffset triggerAt = now.Add(request.TriggerIn);
			ScheduledRequest scheduledRequest = new()
			{
				Id = request.Id,
				TriggerAtUnixTimeInMilliSeconds = triggerAt.ToUnixTimeMilliseconds(),
				TriggerInMilliSeconds = (long)request.TriggerIn.TotalMilliseconds,
				RepeatEveryMilliSeconds = (long)request.RepeatEvery.GetValueOrDefault(Timeout.InfiniteTimeSpan).TotalMilliseconds,
				Notification = serializedNotification
			};

			await _timeSeries
				.AddOrUpdate(_name, request.Id, request.TriggerAt, scheduledRequest.ToByteArray(), cancellationToken)
				.ConfigureAwait(false);

			return Unit.Value;
		}
	}
}