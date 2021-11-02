using System;

using MediatR;

namespace vivego.Collection.EventStore.Delete
{
	public sealed record DeleteRequest : IRequest
	{
		public string StreamId { get; }

		public DeleteRequest(string streamId)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			StreamId = streamId;
		}
	}
}