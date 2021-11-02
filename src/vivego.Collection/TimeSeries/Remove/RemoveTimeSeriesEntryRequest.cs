using System;

using MediatR;

namespace vivego.Collection.TimeSeries.Remove
{
	public sealed record RemoveTimeSeriesEntryRequest : IRequest<bool>
	{
		public string TimeSeriesId { get; }
		public string Id { get; }

		public RemoveTimeSeriesEntryRequest(string timeSeriesId, string id)
		{
			if (string.IsNullOrEmpty(timeSeriesId)) throw new ArgumentException("Value cannot be null or empty.", nameof(timeSeriesId));
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
			TimeSeriesId = timeSeriesId;
			Id = id;
		}
	}
}