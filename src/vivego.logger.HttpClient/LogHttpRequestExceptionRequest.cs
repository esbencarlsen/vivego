using System;
using System.Net.Http;
using MediatR;
using Microsoft.Extensions.Logging;

namespace vivego.logger.HttpClient
{
    public sealed record LogHttpRequestExceptionRequest(
        ILogger Logger,
        HttpRequestMessage HttpRequestMessage,
        Exception Exception) : IRequest;
}