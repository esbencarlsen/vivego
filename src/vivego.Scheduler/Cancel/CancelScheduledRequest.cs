using System;

using MediatR;

namespace vivego.Scheduler.Cancel
{
	public sealed record CancelScheduledRequest : IRequest
	{
		public string Id { get; }
		
		public CancelScheduledRequest(string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));
			Id = id;
		}
	}
}