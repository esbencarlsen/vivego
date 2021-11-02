using MediatR;

namespace vivego.Collection.Queue.TryTakeFirst
{
	public sealed record TryTakeFirstRequest : IRequest<IQueueEntry?>
	{
		public string Id { get; }
		public bool Fast { get; }

		public TryTakeFirstRequest(string id, bool fast)
		{
			Id = id;
			Fast = fast;
		}
	}
}