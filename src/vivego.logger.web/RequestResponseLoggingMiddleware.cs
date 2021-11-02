using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.logger.web
{
	public sealed class RequestResponseLoggingMiddleware : IMiddleware, INamedService
	{
		private readonly IMediator _mediator;
		private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

		public RequestResponseLoggingMiddleware(
			string name,
			IMediator mediator,
			ILogger<RequestResponseLoggingMiddleware> logger)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public string Name { get; }

		public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
		{
			if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
			if (next is null) throw new ArgumentNullException(nameof(next));

			RecordOptions recordOptions = await _mediator
				.Send(new GetLogRequestOptionsRequest(httpContext), httpContext.RequestAborted)
				.ConfigureAwait(false);
			bool recordRequestBody = (recordOptions & RecordOptions.RecordRequestBody) == RecordOptions.RecordRequestBody;

			await using MemoryStream requestStream = new();
			await using ConfiguredAsyncDisposable _ = requestStream.ConfigureAwait(false);
			if (recordRequestBody)
			{
				httpContext.Request.EnableBuffering();
				await httpContext.Request.Body.CopyToAsync(requestStream).ConfigureAwait(false);
				// Reset the request body stream position so the next middleware can read it
				httpContext.Request.Body.Position = 0;
			}

			bool recordResponseBody = (recordOptions & RecordOptions.RecordResponseBody) == RecordOptions.RecordResponseBody;
			await using MemoryStream responseStream = new();
			await using ConfiguredAsyncDisposable __ = responseStream.ConfigureAwait(false);
			Stream originalResponseStream = httpContext.Response.Body;
			if (recordResponseBody)
			{
				httpContext.Response.Body = responseStream;
			}

			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				await next(httpContext).ConfigureAwait(false);
				stopwatch.Stop();
				if (recordResponseBody)
				{
					await requestStream.FlushAsync().ConfigureAwait(false);
					responseStream.Seek(0, SeekOrigin.Begin);
					await responseStream.CopyToAsync(originalResponseStream).ConfigureAwait(false);
					responseStream.Seek(0, SeekOrigin.Begin);
				}

				await _mediator
					.Send(new LogRequest(_logger, httpContext, requestStream, responseStream, stopwatch.Elapsed), httpContext.RequestAborted)
					.ConfigureAwait(false);
			}
			finally
			{
				httpContext.Response.Body = originalResponseStream;
			}
		}
	}
}
