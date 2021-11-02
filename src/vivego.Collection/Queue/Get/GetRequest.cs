using MediatR;

namespace vivego.Collection.Queue.Get
{
	public sealed record GetRequest : IRequest<IQueueEntry?>
	{
		public string Id { get; }
		public long Version { get; }
		
		public GetRequest(string id, long version)
		{
			Id = id;
			Version = version;
		}
	}
}