using MediatR;

namespace vivego.Collection.Queue.Truncate
{
	public sealed record TruncateRequest : IRequest
	{
		public string Id { get; }
		public long? Head { get; }
		public long? Tail { get; }
		public bool Fast { get; }

		public TruncateRequest(string id, long? head, long? tail, bool fast)
		{
			Id = id;
			Head = head;
			Tail = tail;
			Fast = fast;
		}
	}
}