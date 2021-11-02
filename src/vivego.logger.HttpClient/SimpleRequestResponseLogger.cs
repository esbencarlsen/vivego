using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace vivego.logger.HttpClient
{
	public sealed class SimpleRequestResponseLogger :
		IRequestHandler<LogHttpRequestResponseRequest, Unit>,
		IRequestHandler<LogHttpRequestExceptionRequest, Unit>
	{
		private readonly IOptions<ResponseLoggerRequestHandlerOptions> _options;

		public SimpleRequestResponseLogger(IOptions<ResponseLoggerRequestHandlerOptions> options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public Task<Unit> Handle(LogHttpRequestResponseRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.HttpRequestMessage is null) throw new ArgumentNullException(nameof(request.HttpRequestMessage));
			if (request.HttpResponseMessage is null) throw new ArgumentNullException(nameof(request.HttpResponseMessage));

			if (request.Logger.IsEnabled(_options.Value.Level))
			{
				request.Logger.Log(request.HttpResponseMessage.IsSuccessStatusCode ? _options.Value.Level : LogLevel.Error, "{Message}", request.HttpRequestMessage.ToString());
			}

			return Task.FromResult(Unit.Value);
		}

		public Task<Unit> Handle(LogHttpRequestExceptionRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.HttpRequestMessage is null) throw new ArgumentNullException(nameof(request.HttpRequestMessage));

			if (request.Logger.IsEnabled(_options.Value.Level))
			{
				request.Logger.Log(LogLevel.Error, request.Exception, "{Message}", request.HttpRequestMessage.ToString());
			}

			return Task.FromResult(Unit.Value);
		}
	}
}
