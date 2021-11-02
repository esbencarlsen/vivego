using System;
using System.Collections.Generic;

using MediatR;

namespace vivego.Scheduler.GetAll
{
	public sealed record GetAllScheduledRequests : IRequest<IAsyncEnumerable<IScheduledNotification>>
	{
		public DateTimeOffset From { get; }
		public DateTimeOffset To { get; }

		public GetAllScheduledRequests(DateTimeOffset from, DateTimeOffset to)
		{
			From = from;
			To = to;
		}
	}
}