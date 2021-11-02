using System;

using Microsoft.Extensions.Logging;

namespace vivego.logger.web
{
	public sealed class DefaultRequestResponseHandlerOptions
	{
		public LogLevel LogLevel { get; set; } = LogLevel.Debug;
		public Predicate<LogRequest>? Predicate { get; set; }
	}
}