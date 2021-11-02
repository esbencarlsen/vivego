using System;
using System.Linq;

namespace vivego.ServiceInvocation
{
	public sealed record ServiceInvocationLogEntry
	{
		public DateTimeOffset CreatedAt { get; init; }
		public string? Method { get; init; }
		public Uri? Url { get; init; }
		public ILookup<string, string>? RequestHeaders { get; init; }
		public byte[]? RequestPayload { get; init; }
		public int ResponseCode { get; init; }
		public ILookup<string, string>? ResponseHeaders { get; init; }
		public byte[]? ResponsePayload { get; init; }
		public string? Error { get; init; }
		public TimeSpan RequestTime { get; init; }
	}
}