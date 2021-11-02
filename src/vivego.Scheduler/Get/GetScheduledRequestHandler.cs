using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.TimeSeries;
using vivego.Scheduler.Model;
using vivego.Serializer;

namespace vivego.Scheduler.Get
{
	public sealed class GetScheduledRequestHandler : IRequestHandler<GetScheduledRequest, IScheduledNotification?>
	{
		private readonly string _name;
		private readonly ITimeSeries _timeSeries;
		private readonly ISerializer _serializer;

		public GetScheduledRequestHandler(string name, ITimeSeries timeSeries, ISerializer serializer)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_timeSeries = timeSeries ?? throw new ArgumentNullException(nameof(timeSeries));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public async Task<IScheduledNotification?> Handle(GetScheduledRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (string.IsNullOrEmpty(request.Id)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Id));

			ITimeSeriesEntry? timeSeriesEntry = await _timeSeries.Get(_name, request.Id, cancellationToken).ConfigureAwait(false);
			if (timeSeriesEntry is null)
			{
				return null;
			}

			ScheduledRequest scheduledRequest = ScheduledRequest.Parser.ParseFrom(timeSeriesEntry.Data.Data);
			return new ScheduledNotification(scheduledRequest, _serializer);
		}
	}
}
