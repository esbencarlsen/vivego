using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Options;

using vivego.core;

namespace vivego.ServiceInvocation.HttpInvocationHandler
{
	public sealed class HttpInvocationRequestHandler : IRequestHandler<HttpInvocationRequest, HttpResponseMessage>
	{
		public const string ServiceInvocationSecureClient = "ServiceInvocationSecureClient";
		public const string ServiceInvocationInSecureClient = "ServiceInvocationInSecureClient";

		private readonly IOptions<DefaultServiceInvocationOptions> _options;
		private readonly IHttpClientFactory _httpClientFactory;

		public HttpInvocationRequestHandler(
			IOptions<DefaultServiceInvocationOptions> options,
			IHttpClientFactory httpClientFactory)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
		}

		public async Task<HttpResponseMessage> Handle(HttpInvocationRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			// Build request
			using HttpClient httpClient = _httpClientFactory.CreateClient(request.Invocation.VerifyHttpsServerCertificate ? ServiceInvocationSecureClient : ServiceInvocationInSecureClient);
			httpClient.Timeout = request.Invocation.RequestTimeout ??  _options.Value.DefaultRequestTimeout;
			Uri url = request.Invocation.Urls.First();
			using HttpRequestMessage httpRequestMessage = new(new HttpMethod(request.Invocation.Method), url);
			if (request.Invocation.Payload is not null)
			{
				httpRequestMessage.Content = new ByteArrayContent(request.Invocation.Payload);
			}

			foreach (var (key, value) in request.Invocation.Headers.EmptyIfNull())
			{
				AddHeader(httpRequestMessage, key, value.ToArray());
			}

			using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationTokenSource.CancelAfter(request.Invocation.ResponseTimeout ?? _options.Value.DefaultResponseTimeout);

			HttpResponseMessage response = await httpClient
				.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token)
				.ConfigureAwait(false);
				
			return response;
		}
		
		private static void AddHeader(
			HttpRequestMessage httpRequestMessage,
			string key,
			params string[] values)
		{
			if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
			{
				if (httpRequestMessage.Content is not null)
				{
					httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(string.Join(",", values));
					return;
				}
			}

			if (!httpRequestMessage.Headers.TryAddWithoutValidation(key, values))
			{
				httpRequestMessage.Content?.Headers.TryAddWithoutValidation(key, values);
			}
		}
	}
}