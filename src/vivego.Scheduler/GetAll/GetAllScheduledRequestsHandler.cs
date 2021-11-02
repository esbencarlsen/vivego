using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.TimeSeries;
using vivego.Scheduler.Model;
using vivego.Serializer;

namespace vivego.Scheduler.GetAll
{
	public sealed class GetAllScheduledRequestsHandler : IRequestHandler<GetAllScheduledRequests, IAsyncEnumerable<IScheduledNotification>>
	{
		private readonly string _name;
		private readonly ITimeSeries _timeSeries;
		private readonly ISerializer _serializer;

		public GetAllScheduledRequestsHandler(string name, ITimeSeries timeSeries, ISerializer serializer)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_timeSeries = timeSeries ?? throw new ArgumentNullException(nameof(timeSeries));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public Task<IAsyncEnumerable<IScheduledNotification>> Handle(GetAllScheduledRequests request,
			CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			IAsyncEnumerable<IScheduledNotification> asyncEnumerable = _timeSeries
				.GetRange(_name, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, cancellationToken)
				.Select(timeSeriesEntry =>
				{
					ScheduledRequest scheduledRequest = ScheduledRequest.Parser.ParseFrom(timeSeriesEntry.Data.Data);
					IScheduledNotification scheduledNotification = new ScheduledNotification(scheduledRequest, _serializer);
					return scheduledNotification;
				});
			return Task.FromResult(asyncEnumerable);
		}
	}
}
