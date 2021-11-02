using System;

using MediatR;

using vivego.Queue.Model;

namespace vivego.Collection.Queue.SetState
{
	public sealed record SetStateRequest : IRequest
	{
		public string Id { get; }
		public QueueState State { get; }
		public TimeSpan? ExpiresIn { get; }

		public SetStateRequest(string id, QueueState state, TimeSpan? expiresIn)
		{
			Id = id;
			(State, ExpiresIn) = (state, expiresIn);
		}
	}
}