using System;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Retrying;
using vivego.KeyValue.Set;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddRetryingKeyValueStoreBehaviour(this IServiceBuilder builder,
			int retries,
			Func<int, Exception, bool> retryingPredicate)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));
			if (retryingPredicate is null) throw new ArgumentNullException(nameof(retryingPredicate));

			builder.Services.TryAddSingleton(_ => new RetryingKeyValueStoreBehaviour(retries, retryingPredicate));
			builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>>(sp => sp.GetRequiredService<RetryingKeyValueStoreBehaviour>());
			builder.Services.AddSingleton<IPipelineBehavior<GetRequest, KeyValueEntry>>(sp => sp.GetRequiredService<RetryingKeyValueStoreBehaviour>());
			builder.Services.AddSingleton<IPipelineBehavior<DeleteRequest, bool>>(sp => sp.GetRequiredService<RetryingKeyValueStoreBehaviour>());
			builder.Services.AddSingleton<IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>>(sp => sp.GetRequiredService<RetryingKeyValueStoreBehaviour>());

			return builder;
		}
	}
}