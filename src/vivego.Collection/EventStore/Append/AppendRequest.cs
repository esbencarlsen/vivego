using System;
using System.Collections.Generic;
using System.Linq;

using MediatR;

using vivego.core;
using vivego.EventStore;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore.Append
{
	public sealed record AppendRequest : IRequest<Version>
	{
		public string StreamId { get; }
		public long ExpectedVersion { get; }
		public EventData[] EventDatas { get; }

		public AppendRequest(string streamId,
			long expectedVersion,
			IEnumerable<EventData> eventDatas)
		{
			if (string.IsNullOrEmpty(streamId)) throw new ArgumentException("Value cannot be null or empty.", nameof(streamId));
			StreamId = streamId;
			ExpectedVersion = expectedVersion;
			EventDatas = eventDatas.EmptyIfNull().ToArray();
		}
	}
}