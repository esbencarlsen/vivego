using System;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.Scheduler.Cancel;
using vivego.Scheduler.Model;
using vivego.Scheduler.Orleans.Cancel;
using vivego.Scheduler.Orleans.Schedule;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.Scheduler
{
	public static class SchedulerConfigurationExtensions
	{
		public static IServiceBuilder AddOrleansSchedulerPipelineBehavior(this IServiceBuilder builder)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder
				.Services
				.AddSingleton<IPipelineBehavior<CancelScheduledRequest, Unit>, CancelScheduledRequestPipelineBehavior>()
				.AddSingleton<IPipelineBehavior<ScheduledRequest, Unit>, ScheduleRequestPipelineBehavior>();

			return builder;
		}
	}
}