using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;

namespace vivego.MediatR;

internal sealed class LoggingPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
	private readonly ILogger _logger;
	private readonly LogLevel _logLevel;
	private readonly Action<ILogger, TRequest, TResponse> _logCallback;

	public LoggingPipelineBehaviour(
		ILogger logger,
		LogLevel logLevel,
		Action<ILogger, TRequest, TResponse> logCallback)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_logLevel = logLevel;
		_logCallback = logCallback ?? throw new ArgumentNullException(nameof(logCallback));
	}

	public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
	{
		TResponse response = await next().ConfigureAwait(false);
		if (_logger.IsEnabled(_logLevel))
		{
			_logCallback(_logger, request, response);
		}

		return response;
	}
}
