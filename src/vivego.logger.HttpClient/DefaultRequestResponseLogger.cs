using System;
using System.Net.Http;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;

namespace vivego.logger.HttpClient
{
	public sealed class DefaultRequestResponseLogger : IRequestResponseLogger
	{
		private readonly ILogger<DefaultRequestResponseLogger> _logger;
		private readonly IMediator _mediator;

		public DefaultRequestResponseLogger(
			ILogger<DefaultRequestResponseLogger> logger,
			IMediator mediator)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

		public Task Log(HttpRequestMessage httpRequestMessage,
			HttpResponseMessage httpResponseMessage,
			TimeSpan requestResponseTime) =>
			_mediator.Send(new LogHttpRequestResponseRequest(_logger, httpRequestMessage, httpResponseMessage, requestResponseTime));

		public Task Log(HttpRequestMessage httpRequestMessage, Exception exception) =>
			_mediator.Send(new LogHttpRequestExceptionRequest(_logger, httpRequestMessage, exception));
	}
}
