using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Options;

namespace vivego.ServiceInvocation.Invocation
{
	public sealed class RetryPipelineBehaviour : IPipelineBehavior<ServiceInvocationRequest, ServiceInvocationEntryResponse>
	{
		private readonly IMediator _mediator;
		private readonly IOptions<DefaultServiceInvocationOptions> _options;

		public RetryPipelineBehaviour(
			IMediator mediator,
			IOptions<DefaultServiceInvocationOptions> options)
		{
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public async Task<ServiceInvocationEntryResponse> Handle(
			ServiceInvocationRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<ServiceInvocationEntryResponse> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));

			foreach (int retry in Enumerable.Range(0, request.Entry.RetryCount))
			{
				if (request.Entry.RetryUntil.HasValue)
				{
					if (request.Entry.RetryUntil.Value > DateTimeOffset.Now)
					{
						break;
					}
				}

				try
				{
					return await next().ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
				}
				catch (HttpListenerException e)
				{
					Console.WriteLine(e);
					throw;
				}

				TimeSpan[] retryWaitTimes = request.Entry.RetryWaitTimes ?? _options.Value.DefaultRetryDelays;
				TimeSpan retryWaitTime = retry > retryWaitTimes.Length ? retryWaitTimes[^1] : retryWaitTimes[retry];
				await Task.Delay(retryWaitTime, cancellationToken).ConfigureAwait(false);
			}

			if (request.Entry.HttpInvocationFailure is not null)
			{
				ServiceInvocationRequest serviceInvocationRequest = request with
				{
					Entry = request.Entry.HttpInvocationFailure
				};
				return await _mediator
					.Send(serviceInvocationRequest, cancellationToken)
					.ConfigureAwait(false);
			}

			throw new Exception("");
		}
	}
}
