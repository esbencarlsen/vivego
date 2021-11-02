using System;
using System.Net.Http;

using MediatR;

using Microsoft.Extensions.Logging;

namespace vivego.logger.HttpClient
{
	public sealed record LogHttpRequestResponseRequest(
		ILogger Logger,
		HttpRequestMessage HttpRequestMessage,
		HttpResponseMessage HttpResponseMessage,
		TimeSpan RequestResponseTime) : IRequest;
}