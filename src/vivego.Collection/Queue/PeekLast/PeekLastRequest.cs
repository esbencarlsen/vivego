using MediatR;

namespace vivego.Collection.Queue.PeekLast
{
	public sealed record PeekLastRequest : IRequest<IQueueEntry?>
	{
		public string Id { get; }

		public PeekLastRequest(string id)
		{
			Id = id;
		}
	}
}