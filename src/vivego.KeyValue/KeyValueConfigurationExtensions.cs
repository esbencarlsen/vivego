using System;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Behaviours;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;
using vivego.MediatR;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue;

public static class KeyValueConfigurationExtensions
{
	public static IServiceBuilder AddTypedKeyValueStore(this IServiceCollection collection,
		string? name = default,
		string? keyValueStoreName = default)
	{
		if (collection is null) throw new ArgumentNullException(nameof(collection));

		TypedKeyValueStoreBuilder typedKeyValueStoreBuilder = new(name ?? "Default", collection);
		typedKeyValueStoreBuilder.DependsOnNamedService<IKeyValueStore>(keyValueStoreName);

		return typedKeyValueStoreBuilder;
	}

	public static IServiceBuilder AddETagKeyValueBehaviour(this IServiceBuilder builder)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		builder.Services.TryAddSingleton<ETagKeyValueStoreBehavior>();
		builder.Services.AddSingleton<IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>>(sp => sp.GetRequiredService<ETagKeyValueStoreBehavior>());
		builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>>(sp => sp.GetRequiredService<ETagKeyValueStoreBehavior>());
		builder.Services.AddSingleton<IPipelineBehavior<DeleteRequest, bool>>(sp => sp.GetRequiredService<ETagKeyValueStoreBehavior>());

		return builder;
	}

	public static IServiceBuilder AddValidatingKeyValueStoreBehaviour(this IServiceBuilder builder)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		builder.Services.TryAddSingleton<ValidatingKeyValueStoreBehavior>();
		builder.Services.AddSingleton<IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>>(sp => sp.GetRequiredService<ValidatingKeyValueStoreBehavior>());
		builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>>(sp => sp.GetRequiredService<ValidatingKeyValueStoreBehavior>());
		builder.Services.AddSingleton<IPipelineBehavior<GetRequest, KeyValueEntry>>(sp => sp.GetRequiredService<ValidatingKeyValueStoreBehavior>());
		builder.Services.AddSingleton<IPipelineBehavior<DeleteRequest, bool>>(sp => sp.GetRequiredService<ValidatingKeyValueStoreBehavior>());

		return builder;
	}

	public static IServiceBuilder AddLoggingKeyValueStoreBehaviour(this IServiceBuilder builder,
		LogLevel logLevel = LogLevel.Debug)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		builder.AddLoggingPipelineBehaviour<SetRequest, string>(logLevel, (logger, request, response) => logger.Log(logLevel, "Set: '{Key}': eTag: '{ETag}'; result: {Response}", request.Entry.Key, request.Entry.ETag, response));
		builder.AddLoggingPipelineBehaviour<GetRequest, KeyValueEntry>(logLevel, (logger, request, _) => logger.Log(logLevel, "Get: '{Key}'", request.Key));
		builder.AddLoggingPipelineBehaviour<DeleteRequest, bool>(logLevel, (logger, request, response) => logger.Log(logLevel, "Delete: '{Key}'; eTag: '{ETag}'; result: {Response}", request.Entry.Key, request.Entry.ETag, response.ToString()));
		builder.AddLoggingPipelineBehaviour<FeaturesRequest, KeyValueStoreFeatures>(logLevel, (logger, _, _) => logger.Log(logLevel, "GetFeatures"));
		builder.AddLoggingPipelineBehaviour<ClearRequest, Unit>(logLevel, (logger, _, _) => logger.Log(logLevel, "Cleared Store: {Name}", builder.Name));

		return builder;
	}

	public static IServiceBuilder AddRetryingKeyValueStoreBehaviour(this IServiceBuilder builder)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		builder.DependsOn<ILoggerFactory>();
		builder.AddRetryingPipelineBehaviour<SetRequest, string>(sp => DefaultRetryPolicy<string>(nameof(SetRequest), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Retrying")));
		builder.AddRetryingPipelineBehaviour<GetRequest, KeyValueEntry>(sp => DefaultRetryPolicy<KeyValueEntry>(nameof(GetRequest), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Retrying")));
		builder.AddRetryingPipelineBehaviour<DeleteRequest, bool>(sp => DefaultRetryPolicy<bool>(nameof(DeleteRequest), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Retrying")));
		builder.AddRetryingPipelineBehaviour<FeaturesRequest, KeyValueStoreFeatures>(sp => DefaultRetryPolicy<KeyValueStoreFeatures>(nameof(FeaturesRequest), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Retrying")));
		builder.AddRetryingPipelineBehaviour<ClearRequest, Unit>(sp => DefaultRetryPolicy<Unit>(nameof(ClearRequest), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Retrying")));

		return builder;
	}

	private static AsyncRetryPolicy<T> DefaultRetryPolicy<T>(
		string action,
		ILogger logger)
	{
		var sleepDurations = new[]
		{
			TimeSpan.FromMilliseconds(100),
			TimeSpan.FromMilliseconds(200),
			TimeSpan.FromMilliseconds(400),
			TimeSpan.FromMilliseconds(800),
			TimeSpan.FromMilliseconds(1600),
			TimeSpan.FromMilliseconds(3200),
			TimeSpan.FromMilliseconds(6400)
		};
		return Policy
			.Handle<Exception>()
			.OrResult<T>(_ => false)
			.WaitAndRetryAsync(sleepDurations,
				(res, waitTime, @try, _) =>
				{
					logger.LogError(res.Exception,
						$"{{Action}}: {{Try}} / {{{sleepDurations.Length}}}, waiting for {{WaitTime}}", action, @try, waitTime);
				});
	}
}
