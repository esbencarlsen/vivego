using System;

namespace vivego.Collection.TimeSeries
{
	public sealed record DefaultTimeSeriesEntry : ITimeSeriesEntry 
	{
		public string Id { get; }
		public DateTimeOffset Offset { get; }
		public Value Data { get; }

		public DefaultTimeSeriesEntry(string id, Value data, DateTimeOffset offset)
		{
			Id = id;
			Data = data;
			Offset = offset;
		}
	}
}