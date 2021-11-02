using MediatR;

namespace vivego.Collection.EventStore
{
	public sealed class EventStoreVacuumNotification : INotification
	{
		public string? StreamId { get; set; }
	}
}
