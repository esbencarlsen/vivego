using MediatR;

namespace vivego.Collection.TimeSeries.GetCount
{
	public sealed record GetCountTimeSeriesEntriesRequest : IRequest<long>
	{
		public string TimeSeriesId { get; }
		
		public GetCountTimeSeriesEntriesRequest(string timeSeriesId)
		{
			TimeSeriesId = timeSeriesId;
		}
	}
}