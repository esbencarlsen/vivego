using System;

namespace vivego.ServiceInvocation
{
	public sealed class DefaultServiceInvocationOptions
	{
		public TimeSpan DefaultRequestTimeout { get; init; } = TimeSpan.FromMinutes(1);
		public TimeSpan DefaultResponseTimeout { get; init; } = TimeSpan.FromMinutes(1);
		public string HttpRequestHeaderPrefix { get; init; } = "si-";

		public TimeSpan[] DefaultRetryDelays { get; init; } =
		{
			TimeSpan.FromSeconds(30),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(2),
			TimeSpan.FromMinutes(4),
			TimeSpan.FromMinutes(8),
			TimeSpan.FromMinutes(16),
			TimeSpan.FromMinutes(32),
			TimeSpan.FromMinutes(60)
		};
		public TimeSpan HttpLogRetension { get; init; } = TimeSpan.FromHours(3);
	}
}
