using MediatR;

namespace vivego.Collection.TimeSeries.Get
{
	public sealed record GetTimeSeriesEntryRequest : IRequest<ITimeSeriesEntry?>
	{
		public string TimeSeriesId { get; }
		public string Id { get; }
		
		public GetTimeSeriesEntryRequest(string timeSeriesId, string id)
		{
			TimeSeriesId = timeSeriesId;
			Id = id;
		}
	}
}