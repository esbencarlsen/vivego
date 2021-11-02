using System;

using MediatR;

namespace vivego.Collection.EventStore.SetOptions
{
	public sealed record SetOptionsRequest : IRequest
	{
		public string StreamId { get; }
		public EventStreamOptions EventStreamOptions { get; }
		public long ExpiresInSeconds { get; }

		public SetOptionsRequest(string streamId, EventStreamOptions eventStreamOptions, long expiresInSeconds)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			StreamId = streamId;
			EventStreamOptions = eventStreamOptions ?? throw new ArgumentNullException(nameof(eventStreamOptions));
			ExpiresInSeconds = expiresInSeconds;
		}
	}
}