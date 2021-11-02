using System.Collections.Generic;

using MediatR;

namespace vivego.Collection.Queue.GetAll
{
	public sealed record GetAllRequest : IRequest<IAsyncEnumerable<IQueueEntry>>
	{
		public string Id { get; }
		public long? Skip { get; }
		
		public GetAllRequest(string id, long? skip)
		{
			Id = id;
			Skip = skip;
		}
	}
}