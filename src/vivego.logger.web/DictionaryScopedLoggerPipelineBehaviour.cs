using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

using vivego.logger.core;

namespace vivego.logger.web
{
	public sealed class DictionaryScopedLoggerPipelineBehaviour : IPipelineBehavior<LogRequest, Unit>
	{
		private readonly IContentFormatter _contentFormatter;

		public DictionaryScopedLoggerPipelineBehaviour(IContentFormatter contentFormatter)
		{
			_contentFormatter = contentFormatter ?? throw new ArgumentNullException(nameof(contentFormatter));
		}

		public async Task<Unit> Handle(
			LogRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<Unit> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			IHttpResponseTrailersFeature responseTrailersFeature = request.HttpContext.Features.Get<IHttpResponseTrailersFeature>();
			IDictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal)
			{
				{ "method", request.HttpContext.Request.Method },
				{ "url", request.HttpContext.Request.GetDisplayUrl() },
				{ "queryString", request.HttpContext.Request.QueryString },
				{ "scheme", request.HttpContext.Request.Scheme },
				{ "localPort", request.HttpContext.Connection.LocalPort },
				{ "remotePort", request.HttpContext.Connection.RemotePort },
				{ "remoteIpAddress", request.HttpContext.Connection.RemoteIpAddress },
				{ "protocol", request.HttpContext.Request.Protocol },
				{ "headers", MakeHttpHeadersDictionary(request.HttpContext.Request.Headers) },
				{ "content", await MakeContentDictionary(request.HttpContext.Request.ContentType, request.RequestStream, cancellationToken).ConfigureAwait(false) },
				{ "status", request.HttpContext.Response.StatusCode },
				{
					"response", new Dictionary<string, object>(StringComparer.Ordinal)
					{
						{ "headers", MakeHttpHeadersDictionary(request.HttpContext.Response.Headers) },
						{ "content", await MakeContentDictionary(request.HttpContext.Response.ContentType, request.ResponseStream, cancellationToken).ConfigureAwait(false) },
						{ "trailingHeaders", MakeHttpHeadersDictionary(responseTrailersFeature?.Trailers) }
					}
				},
				{ "processingTimeInMs", request.ProcessingTime.TotalMilliseconds }
			};

			using IDisposable _ = request.Logger.BeginScope(dictionary);

			return await next().ConfigureAwait(false);
		}

		private static IDictionary<string, object> MakeHttpHeadersDictionary(IHeaderDictionary? httpHeaders)
		{
			IDictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
			if (httpHeaders is not null)
			{
				foreach ((string key, StringValues value) in httpHeaders)
				{
					dictionary[key] = string.Join(",", value);
				}
			}

			return dictionary;
		}

		private async Task<IDictionary<string, object>> MakeContentDictionary(
			string contentType,
			Stream? stream,
			CancellationToken cancellationToken)
		{
			IDictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
			if (stream is not null)
			{
				dictionary["content"] = await _contentFormatter
					.Format(contentType, stream, cancellationToken)
					.ConfigureAwait(false);
			}

			return dictionary;
		}
	}
}
