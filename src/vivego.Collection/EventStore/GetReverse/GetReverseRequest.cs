using System;
using System.Collections.Generic;

using MediatR;

using vivego.EventStore;

using Range = vivego.EventStore.Range;

namespace vivego.Collection.EventStore.GetReverse
{
	public sealed record GetReverseRequest : IRequest<IAsyncEnumerable<RecordedEvent>>
	{
		public string StreamId { get; }
		public Range Range { get; }

		public GetReverseRequest(string streamId, Range range)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			StreamId = streamId;
			Range = range ?? throw new ArgumentNullException(nameof(range));
		}
	}
}