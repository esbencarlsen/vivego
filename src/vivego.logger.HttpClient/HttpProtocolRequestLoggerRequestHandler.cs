using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using vivego.logger.core;

namespace vivego.logger.HttpClient
{
	public sealed class HttpProtocolRequestLoggerRequestHandler :
		IRequestHandler<LogHttpRequestResponseRequest, Unit>,
		IRequestHandler<LogHttpRequestExceptionRequest, Unit>
	{
		private readonly IContentFormatter _contentFormatter;
		private readonly IOptions<ResponseLoggerRequestHandlerOptions> _options;

		public HttpProtocolRequestLoggerRequestHandler(
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
				await DefaultRequestAction(request.HttpRequestMessage, stringBuilder).ConfigureAwait(false);
				await DefaultResponseAction(stringBuilder, request.HttpResponseMessage, request.RequestResponseTime).ConfigureAwait(false);
				request.Logger.Log(request.HttpResponseMessage.IsSuccessStatusCode ? _options.Value.Level : LogLevel.Error, "{Message}", stringBuilder.ToString());
			}

			return Unit.Value;
		}
		
		public async Task<Unit> Handle(LogHttpRequestExceptionRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			if (request.Logger.IsEnabled(_options.Value.Level))
			{
				StringBuilder stringBuilder = new();
				await DefaultRequestAction(request.HttpRequestMessage, stringBuilder).ConfigureAwait(false);
				request.Logger.Log(LogLevel.Error, request.Exception, "{Message}", stringBuilder.ToString());
			}

			return Unit.Value;
		}

		private async Task DefaultRequestAction(HttpRequestMessage request, StringBuilder sb)
		{
			sb.AppendLine("Request:");
			sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} HTTP/{2}", request.Method, request.RequestUri, request.Version);
			sb.AppendLine();

			foreach ((string key, IEnumerable<string> value) in request.Headers)
			{
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", key, string.Join(",", value));
				sb.AppendLine();
			}

			await LogContent(request.Content, sb).ConfigureAwait(false);
		}

		private async Task DefaultResponseAction(StringBuilder sb, HttpResponseMessage response, TimeSpan requestResponseTime)
		{
			sb.AppendLine();
			sb.AppendFormat(CultureInfo.InvariantCulture, "Response in {0}ms:", requestResponseTime.TotalMilliseconds);
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendFormat(CultureInfo.InvariantCulture, "HTTP/{0} {1}", response.Version, response.StatusCode);
			sb.AppendLine();
			foreach ((string key, IEnumerable<string> value) in response.Headers)
			{
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", key, string.Join(",", value));
				sb.AppendLine();
			}

			await LogContent(response.Content, sb).ConfigureAwait(false);
		}

		private async Task LogContent(HttpContent? httpContent, StringBuilder sb)
		{
			if (httpContent is null)
			{
				return;
			}

			foreach ((string key, IEnumerable<string> value) in httpContent.Headers)
			{
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", key, string.Join(",", value));
				sb.AppendLine();
			}

			if (httpContent.Headers.ContentType is not null)
			{
				string content = await _contentFormatter
					.Format(httpContent.Headers.ContentType.ToString(),
						await httpContent.ReadAsStreamAsync().ConfigureAwait(false),
						CancellationToken.None)
					.ConfigureAwait(false);
				if (!string.IsNullOrEmpty(content))
				{
					sb.AppendLine();
					sb.AppendLine(content);
				}
			}
		}
	}
}