using System;

using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.TimeSeries;
using vivego.Serializer;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Scheduler
{
	public static class SchedulerConfigurationExtensions
	{
		public static IServiceBuilder AddScheduler(this IServiceCollection collection,
			string? schedulerName = default,
			string? timeSeriesProviderName = default,
			string? serializerName = default,
			Action<DefaultSchedulerOptions>? configure = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			SchedulerServiceBuilder schedulerServiceBuilder = new(schedulerName ?? "Default", collection);
			schedulerServiceBuilder.DependsOnNamedService<ITimeSeries>(timeSeriesProviderName);
			schedulerServiceBuilder.DependsOnNamedService<ISerializer>(serializerName);

			schedulerServiceBuilder.Services
				.AddOptions<DefaultSchedulerOptions>()
				.Configure(options => 
				{
					configure?.Invoke(options);
				});

			return schedulerServiceBuilder;
		}
	}
}