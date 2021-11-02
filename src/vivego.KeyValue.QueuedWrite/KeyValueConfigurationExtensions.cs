using System;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.QueuedWrite;
using vivego.KeyValue.Set;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddQueuedWritePipelineBehavior(this IServiceBuilder builder,
			int maxDegreeOfParallelism = 1,
			bool queueReads = false)
		{
			if (maxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
			if (maxDegreeOfParallelism > 100) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
			if (builder is null) throw new ArgumentNullException(nameof(builder));

			builder.DependsOn<ILogger<QueuedWritePipelineBehavior>>();
			builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<QueuedWritePipelineBehavior>(sp, maxDegreeOfParallelism));
			builder.Services.AddSingleton<IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>>(sp => sp.GetRequiredService<QueuedWritePipelineBehavior>());
			builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>>(sp => sp.GetRequiredService<QueuedWritePipelineBehavior>());
			builder.Services.AddSingleton<IPipelineBehavior<DeleteRequest, bool>>(sp => sp.GetRequiredService<QueuedWritePipelineBehavior>());
			if (queueReads)
			{
				builder.Services.AddSingleton<IPipelineBehavior<GetRequest, KeyValueEntry>>(sp => sp.GetRequiredService<QueuedWritePipelineBehavior>());
			}

			return builder;
		}
	}
}
