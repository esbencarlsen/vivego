using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.logger.core;

namespace vivego.logger.HttpClient
{
	public sealed class DictionaryScopedRequestResponseBehaviour : IPipelineBehavior<LogHttpRequestResponseRequest, Unit>
	{
		private readonly IContentFormatter _contentFormatter;

		public DictionaryScopedRequestResponseBehaviour(IContentFormatter contentFormatter)
		{
			_contentFormatter = contentFormatter ?? throw new ArgumentNullException(nameof(contentFormatter));
		}

		public async Task<Unit> Handle(LogHttpRequestResponseRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<Unit> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.HttpRequestMessage is null) throw new ArgumentNullException(nameof(request.HttpRequestMessage));
			if (request.HttpResponseMessage is null) throw new ArgumentNullException(nameof(request.HttpResponseMessage));

			IDictionary<string, object> dictionary = await Build(request.HttpRequestMessage,
					request.HttpResponseMessage,
					request.RequestResponseTime)
				.ConfigureAwait(false);
			using IDisposable _ = request.Logger.BeginScope(dictionary);
			return await next().ConfigureAwait(false);
		}

		private async Task<IDictionary<string, object>> Build(
			HttpRequestMessage httpRequestMessage,
			HttpResponseMessage httpResponseMessage,
			TimeSpan requestResponseTime)
		{
			if (httpRequestMessage is null) throw new ArgumentNullException(nameof(httpRequestMessage));
			if (httpResponseMessage is null) throw new ArgumentNullException(nameof(httpResponseMessage));
			return new Dictionary<string, object>(StringComparer.Ordinal)
			{
				{"method", httpRequestMessage.Method.Method},
				{"url", httpRequestMessage.RequestUri?.ToString() ?? string.Empty},
				{"version", httpRequestMessage.Version.ToString() ?? string.Empty},
				{"headers", MakeHttpHeadersDictionary(httpRequestMessage.Headers)},
				{"content", await MakeContentDictionary(httpRequestMessage.Content).ConfigureAwait(false)},
				{"status", httpResponseMessage.StatusCode},
				{"reason", httpResponseMessage.ReasonPhrase ?? string.Empty},
				{
					"response", new Dictionary<string, object>(StringComparer.Ordinal)
					{
						{"headers", MakeHttpHeadersDictionary(httpResponseMessage.Headers)},
						{"content", await MakeContentDictionary(httpResponseMessage.Content).ConfigureAwait(false)},
						{"trailingHeaders", MakeHttpHeadersDictionary(httpResponseMessage.TrailingHeaders)},
					}
				},
				{"requestResponseTimeInMs", requestResponseTime.TotalMilliseconds}
			};
		}

		private static IDictionary<string, object> MakeHttpHeadersDictionary(IEnumerable<KeyValuePair<string, IEnumerable<string>>> httpHeaders)
		{
			if (httpHeaders is null) throw new ArgumentNullException(nameof(httpHeaders));
			IDictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
			foreach ((string key, IEnumerable<string> value) in httpHeaders)
			{
				dictionary[key] = string.Join(",", value);
			}

			return dictionary;
		}

		private async Task<IDictionary<string, object>> MakeContentDictionary(HttpContent? httpContent)
		{
			IDictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
			if (httpContent is not null)
			{
				dictionary["headers"] = MakeHttpHeadersDictionary(httpContent.Headers);
				dictionary["content"] = await _contentFormatter
					.Format(httpContent.Headers?.ContentType?.ToString() ?? string.Empty,
						await httpContent.ReadAsStreamAsync().ConfigureAwait(false),
						CancellationToken.None)
					.ConfigureAwait(false);
			}

			return dictionary;
		}
	}
}
