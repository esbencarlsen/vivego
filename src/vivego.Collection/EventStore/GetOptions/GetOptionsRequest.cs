using System;

using MediatR;

namespace vivego.Collection.EventStore.GetOptions
{
	public sealed record GetOptionsRequest : IRequest<EventStreamOptions>
	{
		public string StreamId { get; }

		public GetOptionsRequest(string streamId)
		{
			if (string.IsNullOrWhiteSpace(streamId)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(streamId));
			StreamId = streamId;
		}
	}
}