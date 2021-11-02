using MediatR;

using vivego.Queue.Model;

namespace vivego.Collection.Queue.GetState
{
	public sealed record GetStateRequest : IRequest<QueueState>
	{
		public string Id { get; }

		public GetStateRequest(string id)
		{
			Id = id;
		}
	}
}