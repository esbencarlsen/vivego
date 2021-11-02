using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using vivego.logger.core;

namespace vivego.logger.web
{
	public sealed class DefaultRequestResponseHandler :
		IRequestHandler<GetLogRequestOptionsRequest, RecordOptions>,
		IRequestHandler<LogRequest, Unit>
	{
		private readonly IOptions<DefaultRequestResponseHandlerOptions> _options;
		private readonly IContentFormatter _contentFormatter;

		public DefaultRequestResponseHandler(
			IOptions<DefaultRequestResponseHandlerOptions> options,
			IContentFormatter contentFormatter)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_contentFormatter = contentFormatter ?? throw new ArgumentNullException(nameof(contentFormatter));
		}

		public Task<RecordOptions> Handle(GetLogRequestOptionsRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.HttpContext is null) throw new ArgumentNullException(nameof(request.HttpContext));

			if (request.HttpContext.WebSockets.IsWebSocketRequest)
			{
				return Task.FromResult(RecordOptions.None);
			}

			if (IsLoggerRequest(request.HttpContext.Request.ContentType))
			{
				return Task.FromResult(RecordOptions.RecordRequestBody | RecordOptions.RecordResponseBody);
			}

			return Task.FromResult(RecordOptions.RecordResponseBody);
		}

		public async Task<Unit> Handle(LogRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.HttpContext is null) throw new ArgumentNullException(nameof(request.HttpContext));

			if (!request.Logger.IsEnabled(_options.Value.LogLevel))
			{
				return Unit.Value;
			}

			if (_options.Value.Predicate is not null && !_options.Value.Predicate(request))
			{
				return Unit.Value;
			}

			StringBuilder stringBuilder = new();
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2}", request.HttpContext.Request.Method, request.HttpContext.Request.GetDisplayUrl(), request.HttpContext.Request.Protocol);
			stringBuilder.AppendLine();
			foreach ((string key, StringValues value) in request.HttpContext.Request.Headers)
			{
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", key, value);
				stringBuilder.AppendLine();
			}

			if (request.RequestStream is not null)
			{
				string requestContent = await _contentFormatter.Format(request.HttpContext.Request.ContentType,
						request.RequestStream,
						request.HttpContext.RequestAborted)
					.ConfigureAwait(false);
				if (!string.IsNullOrEmpty(requestContent))
				{
					stringBuilder.AppendLine("Request Content:");
					stringBuilder.AppendLine(requestContent);
				}
			}

			stringBuilder.AppendLine();
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Response in {0}ms:", request.ProcessingTime.TotalMilliseconds);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}", request.HttpContext.Request.Protocol, request.HttpContext.Response.StatusCode);
			stringBuilder.AppendLine();
			foreach ((string key, StringValues value) in request.HttpContext.Response.Headers)
			{
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", key, value);
				stringBuilder.AppendLine();
			}

			if (request.ResponseStream is not null && IsLoggerRequest(request.HttpContext.Response.ContentType))
			{
				string responseContent = await _contentFormatter.Format(request.HttpContext.Response.ContentType,
						request.ResponseStream,
						request.HttpContext.RequestAborted)
					.ConfigureAwait(false);
				if (!string.IsNullOrEmpty(responseContent))
				{
					stringBuilder.AppendLine("Response Content:");
					stringBuilder.AppendLine(responseContent);
				}
			}

			stringBuilder.AppendLine();
			request.Logger.Log(_options.Value.LogLevel, stringBuilder.ToString());

			return Unit.Value;
		}

		private static bool IsLoggerRequest(string contentType)
		{
			if (string.IsNullOrEmpty(contentType))
			{
				return false;
			}

			return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
				|| contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
				|| contentType.Contains("txt", StringComparison.OrdinalIgnoreCase)
				|| contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
				|| contentType.Contains("urlencoded", StringComparison.OrdinalIgnoreCase);
		}
	}
}
