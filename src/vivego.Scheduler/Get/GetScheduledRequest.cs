using System;

using MediatR;

namespace vivego.Scheduler.Get
{
	public sealed record GetScheduledRequest : IRequest<IScheduledNotification?>
	{
		public string Id { get; }

		public GetScheduledRequest(string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
			Id = id;
		}
	}
}