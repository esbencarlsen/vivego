using System;
using System.IO;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace vivego.logger.web
{
	public sealed record LogRequest : IRequest
	{
		public ILogger Logger { get; }
		public HttpContext HttpContext { get; }
		public Stream? RequestStream { get; }
		public Stream? ResponseStream { get; }
		public TimeSpan ProcessingTime { get; }

		public LogRequest(
			ILogger logger,
			HttpContext httpContext,
			Stream? requestStream,
			Stream? responseStream,
			TimeSpan processingTime)
		{
			Logger = logger;
			HttpContext = httpContext;
			RequestStream = requestStream;
			ResponseStream = responseStream;
			ProcessingTime = processingTime;
		}
	}
}