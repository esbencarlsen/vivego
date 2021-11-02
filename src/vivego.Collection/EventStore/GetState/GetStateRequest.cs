using System;

using MediatR;

using vivego.EventStore;

namespace vivego.Collection.EventStore.GetState
{
	public sealed record GetStateRequest : IRequest<EventStoreState>
	{
		public string StreamId { get; }

		public GetStateRequest(string streamId)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			StreamId = streamId;
		}
	}
}