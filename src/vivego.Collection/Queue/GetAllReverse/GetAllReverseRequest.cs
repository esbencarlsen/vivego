using System.Collections.Generic;

using MediatR;

namespace vivego.Collection.Queue.GetAllReverse
{
	public sealed record GetAllReverseRequest : IRequest<IAsyncEnumerable<IQueueEntry>>
	{
		public string Id { get; }
		public long? Skip { get; }
		
		public GetAllReverseRequest(string id, long? skip)
		{
			Id = id;
			Skip = skip;
		}
	}
}