// using System;
// using System.Linq;
// using System.Net.Http;
// using System.Net.Http.Headers;
// using System.Threading;
// using System.Threading.Tasks;
//
// using MediatR;
//
// using Microsoft.Extensions.Options;
//
// using vivego.core;
//
// namespace vivego.ServiceInvocation.Invocation
// {
// 	public sealed class ServiceInvocationEntryRequestHandler : IRequestHandler<ServiceInvocationRequest, ServiceInvocationEntryResponse>
// 	{
// 		public const string ServiceInvocationSecureClient = "ServiceInvocationSecureClient";
// 		public const string ServiceInvocationInSecureClient = "ServiceInvocationInSecureClient";
//
// 		private readonly IOptions<DefaultServiceInvocationOptions> _options;
// 		private readonly IHttpClientFactory _httpClientFactory;
//
// 		public ServiceInvocationEntryRequestHandler(
// 			IOptions<DefaultServiceInvocationOptions> options,
// 			IHttpClientFactory httpClientFactory)
// 		{
// 			_options = options ?? throw new ArgumentNullException(nameof(options));
// 			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
// 		}
//
// 		public async Task<ServiceInvocationEntryResponse> Handle(ServiceInvocationRequest request, CancellationToken cancellationToken)
// 		{
// 			if (request is null) throw new ArgumentNullException(nameof(request));
// 			if (request.Entry is null) throw new ArgumentNullException(nameof(request));
//
// 			// Build request
// 			using HttpClient httpClient = _httpClientFactory.CreateClient(request.Entry.VerifyHttpsServerCertificate ? ServiceInvocationSecureClient : ServiceInvocationInSecureClient);
// 			httpClient.Timeout = request.Entry.HttpInvocation.RequestTimeout ??  _options.Value.DefaultRequestTimeout;
// 			Uri url = request.Entry.HttpInvocation.Urls.First();
// 			using HttpRequestMessage httpRequestMessage = new(new HttpMethod(request.Entry.HttpInvocation.Method), url);
// 			if (request.Entry.HttpInvocation.Payload is not null)
// 			{
// 				httpRequestMessage.Content = new ByteArrayContent(request.Entry.HttpInvocation.Payload);
// 			}
//
// 			foreach (var (key, value) in request.Entry.HttpInvocation.Headers.EmptyIfNull())
// 			{
// 				AddHeader(httpRequestMessage, key, value.ToArray());
// 			}
//
// 			using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
// 			cancellationTokenSource.CancelAfter(request.Entry.HttpInvocation.ResponseTimeout ?? _options.Value.DefaultResponseTimeout);
//
// 			HttpResponseMessage response = await httpClient
// 				.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token)
// 				.ConfigureAwait(false);
//
// 			return new ServiceInvocationEntryResponse(response);
// 		}
//
// 		private static void AddHeader(
// 			HttpRequestMessage httpRequestMessage,
// 			string key,
// 			params string[] values)
// 		{
// 			if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
// 			{
// 				if (httpRequestMessage.Content is not null)
// 				{
// 					httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(string.Join(",", values));
// 					return;
// 				}
// 			}
//
// 			if (!httpRequestMessage.Headers.TryAddWithoutValidation(key, values))
// 			{
// 				httpRequestMessage.Content?.Headers.TryAddWithoutValidation(key, values);
// 			}
// 		}
// 	}
// }
