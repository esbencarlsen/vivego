using System;

using MediatR;

namespace vivego.Collection.TimeSeries.Add
{
	public sealed record AddTimeSeriesEntryRequest : IRequest
	{
		public string TimeSeriesId { get; }
		public DateTimeOffset DateTimeOffset { get; }
		public string Id { get; }
		public byte[] Data { get; }

		public AddTimeSeriesEntryRequest(string timeSeriesId, DateTimeOffset dateTimeOffset, string id, byte[] data)
		{
			if (string.IsNullOrEmpty(timeSeriesId)) throw new ArgumentException("Value cannot be null or empty.", nameof(timeSeriesId));
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));

			TimeSeriesId = timeSeriesId;
			DateTimeOffset = dateTimeOffset;
			Id = id;
			Data = data ?? throw new ArgumentNullException(nameof(data));
		}
	}
}