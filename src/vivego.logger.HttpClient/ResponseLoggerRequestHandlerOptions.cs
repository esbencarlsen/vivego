using Microsoft.Extensions.Logging;

namespace vivego.logger.HttpClient
{
	public sealed class ResponseLoggerRequestHandlerOptions
	{
		public LogLevel Level { get; set; } = LogLevel.Debug;
	}
}
