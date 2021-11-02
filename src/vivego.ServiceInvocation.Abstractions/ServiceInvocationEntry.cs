using System;

namespace vivego.ServiceInvocation
{
	public sealed record ServiceInvocationEntry
	{
		public HttpInvocation HttpInvocation { get; }
		public ServiceInvocationEntry? HttpInvocationFailure { get; init; }
		public int RetryCount { get; } = 3;
		public DateTimeOffset? RetryUntil { get; init; }
		public TimeSpan[]? RetryWaitTimes { get; init; }
		public bool ClearQueue { get; init; }

		public ServiceInvocationEntry(HttpInvocation httpInvocation) => HttpInvocation = httpInvocation;
	}
}