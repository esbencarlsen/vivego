using System;
using System.Collections.Generic;

using MediatR;

namespace vivego.Collection.TimeSeries.GetRange
{
	public sealed record GetRangeTimeSeriesEntriesRequest : IRequest<IAsyncEnumerable<ITimeSeriesEntry>>
	{
		public string TimeSeriesId { get; }
		public DateTimeOffset From { get; }
		public DateTimeOffset To { get; }

		public GetRangeTimeSeriesEntriesRequest(string timeSeriesId, DateTimeOffset from, DateTimeOffset to)
		{
			TimeSeriesId = timeSeriesId;
			From = from;
			To = to;
		}
	}
}