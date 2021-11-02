using MediatR;

namespace vivego.Collection.Queue.PeekFirst
{
	public sealed record PeekFirstRequest : IRequest<IQueueEntry?>
	{
		public string Id { get; }

		public PeekFirstRequest(string id)
		{
			Id = id;
		}
	}
}