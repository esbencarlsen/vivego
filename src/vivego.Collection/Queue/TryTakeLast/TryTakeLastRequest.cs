using MediatR;

namespace vivego.Collection.Queue.TryTakeLast
{
	public sealed record TryTakeLastRequest : IRequest<IQueueEntry?>
	{
		public string Id { get; }
		public bool Fast { get; }

		public TryTakeLastRequest(string id, bool fast)
		{
			Id = id;
			Fast = fast;
		}
	}
}