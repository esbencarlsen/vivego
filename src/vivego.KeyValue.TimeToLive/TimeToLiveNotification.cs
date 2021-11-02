using MediatR;

namespace vivego.KeyValue.TimeToLive
{
	public sealed class TimeToLiveNotification : INotification
	{
		public string Key { get; set; } = default!;
		public string ETag { get; set; } = default!;
	}
}
