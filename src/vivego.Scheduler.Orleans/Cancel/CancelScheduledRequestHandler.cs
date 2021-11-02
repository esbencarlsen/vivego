using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans;
using Orleans.Runtime;

using vivego.Scheduler.Cancel;

namespace vivego.Scheduler.Orleans.Cancel
{
	public sealed class CancelScheduledRequestPipelineBehavior : IPipelineBehavior<CancelScheduledRequest, Unit>
	{
		private readonly IClusterClient _clusterClient;
		private readonly IScheduler _scheduler;

		public CancelScheduledRequestPipelineBehavior(
			IClusterClient clusterClient,
			IScheduler scheduler)
		{
			_clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
			_scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
		}

		public async Task<Unit> Handle(CancelScheduledRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<Unit> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			if (RequestContext.Get(nameof(SchedulerGrain)) is bool inGrain && inGrain)
			{
				return await next().ConfigureAwait(false);
			}

			await _clusterClient
				.GetGrain<ISchedulerGrain>(_scheduler.Name)
				.Cancel(request.Id)
				.ConfigureAwait(false);

			return Unit.Value;
		}
	}
}
