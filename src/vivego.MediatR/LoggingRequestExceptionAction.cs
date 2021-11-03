using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR.Pipeline;

using Microsoft.Extensions.Logging;

namespace vivego.MediatR;

internal sealed class LoggingRequestExceptionAction<T> : IRequestExceptionAction<T, Exception> where T : notnull
{
	private readonly Func<T, string> _errorMessageFactory;
	private readonly ILogger<LoggingRequestExceptionAction<T>> _logger;

	public LoggingRequestExceptionAction(
		Func<T, string> errorMessageFactory,
		ILogger<LoggingRequestExceptionAction<T>> logger)
	{
		_errorMessageFactory = errorMessageFactory ?? throw new ArgumentNullException(nameof(errorMessageFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Task Execute(T request, Exception exception, CancellationToken cancellationToken)
	{
		if (request is null) throw new ArgumentNullException(nameof(request));
		string errorMessage  = _errorMessageFactory(request);
		_logger.LogError(exception, errorMessage);
		return Task.CompletedTask;
	}
}
