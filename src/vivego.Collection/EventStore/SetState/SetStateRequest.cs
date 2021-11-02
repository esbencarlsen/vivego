using System;

using MediatR;

using vivego.EventStore;

namespace vivego.Collection.EventStore.SetState
{
	public sealed record SetStateRequest : IRequest
	{
		public string StreamId { get; }
		public EventStoreState State { get; }

		public SetStateRequest(string streamId, EventStoreState eventStoreState)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			StreamId = streamId;
			State = eventStoreState;
		}
	}
}