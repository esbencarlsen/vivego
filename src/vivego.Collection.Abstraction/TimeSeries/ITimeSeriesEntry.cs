using System;

namespace vivego.Collection.TimeSeries
{
	public interface ITimeSeriesEntry
	{
		string Id { get; }
		DateTimeOffset Offset { get; }
		Value Data { get; }
	}
}