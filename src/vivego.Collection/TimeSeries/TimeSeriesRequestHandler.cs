namespace vivego.Collection.TimeSeries
{
	public static class TimeSeriesRequestHandler
	{
		public static string MakeKey(string timeSeriesId, string id)
		{
			return $"{timeSeriesId}_{id}";
		}
	}
}