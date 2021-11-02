using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using vivego.logger.core;

namespace vivego.logger.HttpClient
{
	public sealed class DefaultRequestResponseLoggerRequestHandler :
		IRequestHandler<LogHttpRequestResponseRequest, Unit>,
		IRequestHandler<LogHttpRequestExceptionRequest, Unit>
	{
		private readonly IContentFormatter _contentFormatter;
		private readonly IOptions<ResponseLoggerRequestHandlerOptions> _options;

		public DefaultRequestResponseLoggerRequestHandler(
			IContentFormatter contentFormatter,
			IOptions<ResponseLoggerRequestHandlerOptions> options)
		{
			_contentFormatter = contentFormatter ?? throw new ArgumentNullException(nameof(contentFormatter));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public async Task<Unit> Handle(LogHttpRequestResponseRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.HttpRequestMessage is null) throw new ArgumentNullException(nameof(request.HttpRequestMessage));
			if (request.HttpResponseMessage is null) throw new ArgumentNullException(nameof(request.HttpResponseMessage));
			if (request.Logger.IsEnabled(_options.Value.Level))
			{
				StringBuilder stringBuilder = new();
				stringBuilder.AppendLine("Request:");
				stringBuilder.AppendLine(request.HttpRequestMessage.ToString());
				if (request.HttpRequestMessage.Content is not null)
				{
					string requestContent = await _contentFormatter
						.Format(request.HttpRequestMessage.Content.Headers.ContentType?.ToString() ?? string.Empty,
							await request.HttpRequestMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
							CancellationToken.None)
						.ConfigureAwait(false);
					stringBuilder.AppendLine(requestContent);
				}

				stringBuilder.AppendLine();
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Response in: {0}", request.RequestResponseTime);
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(request.HttpResponseMessage.ToString());
				var content = await _contentFormatter
					.Format(request.HttpResponseMessage.Content.Headers.ContentType?.ToString() ?? "Unknown",
						await request.HttpResponseMessage.Content.ReadAsStreamAsync(cancellationToken)
							.ConfigureAwait(false),
						CancellationToken.None)
					.ConfigureAwait(false);
				stringBuilder.AppendLine(content);

				stringBuilder.AppendLine();
				request.Logger.Log(request.HttpResponseMessage.IsSuccessStatusCode
						? _options.Value.Level
						: LogLevel.Error,
					stringBuilder.ToString());
			}

			return Unit.Value;
		}

		public async Task<Unit> Handle(LogHttpRequestExceptionRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			if (request.Logger.IsEnabled(_options.Value.Level))
			{
				StringBuilder stringBuilder = new();
				stringBuilder.AppendLine("Request:");
				stringBuilder.AppendLine(request.HttpRequestMessage.ToString());
				if (request.HttpRequestMessage.Content is not null)
				{
					string requestContent = await _contentFormatter
						.Format(request.HttpRequestMessage.Content.Headers.ContentType?.ToString() ?? string.Empty,
							await request.HttpRequestMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
							CancellationToken.None)
						.ConfigureAwait(false);
					stringBuilder.AppendLine(requestContent);
				}

				stringBuilder.AppendLine();
				request.Logger.Log(LogLevel.Error, request.Exception, "{Message}", stringBuilder.ToString());
			}

			return Unit.Value;
		}
	}
}
