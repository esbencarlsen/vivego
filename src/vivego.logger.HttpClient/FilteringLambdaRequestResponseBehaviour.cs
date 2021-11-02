using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

namespace vivego.logger.HttpClient
{
	public sealed class FilteringLambdaRequestResponseBehaviour : IPipelineBehavior<LogHttpRequestResponseRequest, Unit>
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage, TimeSpan, Task<bool>> _lambdaFilter;

		public FilteringLambdaRequestResponseBehaviour(Func<HttpRequestMessage, HttpResponseMessage, TimeSpan, Task<bool>> lambdaFilter)
		{
			_lambdaFilter = lambdaFilter ?? throw new ArgumentNullException(nameof(lambdaFilter));
		}

		public Task<bool> Allow(HttpRequestMessage httpRequestMessage, HttpResponseMessage httpResponseMessage, TimeSpan requestResponseTime) =>
			_lambdaFilter(httpRequestMessage, httpResponseMessage, requestResponseTime);

		public async Task<Unit> Handle(LogHttpRequestResponseRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<Unit> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));
			if (request.HttpRequestMessage is null) throw new ArgumentNullException(nameof(request.HttpRequestMessage));
			if (request.HttpResponseMessage is null) throw new ArgumentNullException(nameof(request.HttpResponseMessage));

			if (!await Allow(request.HttpRequestMessage, request.HttpResponseMessage, request.RequestResponseTime).ConfigureAwait(false))
			{
				return Unit.Value;
			}

			await next().ConfigureAwait(false);
			return Unit.Value;
		}
	}
}
