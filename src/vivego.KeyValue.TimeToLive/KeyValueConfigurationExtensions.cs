using System;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using vivego.KeyValue.Set;
using vivego.KeyValue.TimeToLive;
using vivego.Scheduler;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddTimeToLiveKeyValueStoreBehaviour(this IServiceBuilder builder,
			string? schedulerName = default)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.DependsOnNamedService<IScheduler>(schedulerName);
			builder.Services.TryAddSingleton<TimeToLiveKeyValueStoreBehaviour>();
			builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>, TimeToLiveKeyValueStoreBehaviour>();
			builder.Services.AddSingleton<INotificationHandler<TimeToLiveNotification>, TimeToLiveKeyValueStoreBehaviour>();
			builder.Map<INotificationHandler<TimeToLiveNotification>>();

			return builder;
		}
	}
}