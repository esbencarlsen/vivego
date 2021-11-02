using System.Collections.Generic;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using vivego.MediatR;
using vivego.Scheduler.Cancel;
using vivego.Scheduler.Get;
using vivego.Scheduler.GetAll;
using vivego.Scheduler.Schedule;
using vivego.ServiceBuilder;

namespace vivego.Scheduler
{
	public sealed class SchedulerServiceBuilder : DefaultServiceBuilder<IScheduler>
	{
		public SchedulerServiceBuilder(
			string? name,
			IServiceCollection serviceServices) : base(name ?? "Default", serviceServices)
		{
			TryAddMediatR(serviceServices);
			serviceServices.TryAddSingleton<ISchedulerDispatcher, MediatrSchedulerDispatcher>();
			DependsOn<ISchedulerDispatcher>();

			string innerName = name ?? "Default";
			DependsOn<ILogger<DefaultScheduler>>();
			Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<DefaultScheduler>(sp, innerName));
			Services.AddSingleton<IScheduler>(sp => sp.GetRequiredService<DefaultScheduler>());
			Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DefaultScheduler>());
			Map<IHostedService>();

			Services.AddSingleton<IRequestHandler<ScheduleRequest, Unit>>(sp => ActivatorUtilities.CreateInstance<ScheduleRequestHandler>(sp, innerName));
			Services.AddSingleton<IRequestHandler<CancelScheduledRequest, Unit>>(sp => ActivatorUtilities.CreateInstance<CancelScheduledRequestHandler>(sp, innerName));
			Services.AddSingleton<IRequestHandler<GetScheduledRequest, IScheduledNotification?>>(sp => ActivatorUtilities.CreateInstance<GetScheduledRequestHandler>(sp, innerName));
			Services.AddSingleton<IRequestHandler<GetAllScheduledRequests, IAsyncEnumerable<IScheduledNotification>>>(sp => ActivatorUtilities.CreateInstance<GetAllScheduledRequestsHandler>(sp, innerName));

			this.AddExceptionLoggingPipelineBehaviour<ScheduleRequest>(request => $"Error while processing {nameof(ScheduleRequest)} for key: {request?.Id}");
			this.AddExceptionLoggingPipelineBehaviour<CancelScheduledRequest>(request => $"Error while processing {nameof(CancelScheduledRequest)} for key: {request?.Id}");
			this.AddExceptionLoggingPipelineBehaviour<GetScheduledRequest>(request => $"Error while processing {nameof(GetScheduledRequest)} for key: {request?.Id}");
			this.AddExceptionLoggingPipelineBehaviour<GetAllScheduledRequests>(_ => $"Error while processing {nameof(GetAllScheduledRequests)}");
		}
	}
}
