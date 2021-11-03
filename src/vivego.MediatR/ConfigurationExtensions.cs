using System;

using MediatR;
using MediatR.Pipeline;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Polly;

using vivego.core;
using vivego.core.Actors;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.MediatR;

public static class ConfigurationExtensions
{
	public static IServiceCollection AddExceptionLoggingPipelineBehaviour<T>(this IServiceCollection serviceCollection,
		Func<T, string> errorMessageFactory) where T : notnull
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

		serviceCollection.AddSingleton<IRequestExceptionAction<T, Exception>>(
			sp => ActivatorUtilities.CreateInstance<LoggingRequestExceptionAction<T>>(sp, errorMessageFactory));

		return serviceCollection;
	}

	public static IServiceBuilder AddExceptionLoggingPipelineBehaviour<T>(this IServiceBuilder serviceBuilder,
		Func<T, string> errorMessageFactory) where T : notnull
	{
		if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));

		serviceBuilder.DependsOn<ILogger<LoggingRequestExceptionAction<T>>>();
		serviceBuilder.Services.AddExceptionLoggingPipelineBehaviour(errorMessageFactory);

		return serviceBuilder;
	}

	public static IServiceCollection AddLoggingPipelineBehaviour<TRequest, TResponse>(this IServiceCollection serviceCollection,
		LogLevel logLevel,
		Action<ILogger, TRequest, TResponse> logCallback) where TRequest : notnull
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (logCallback is null) throw new ArgumentNullException(nameof(logCallback));

		serviceCollection.AddSingleton<IPipelineBehavior<TRequest, TResponse>>(sp =>
			ActivatorUtilities.CreateInstance<LoggingPipelineBehaviour<TRequest, TResponse>>(sp,
				sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(LoggingPipelineBehaviour<TRequest, TResponse>).Name),
				logLevel,
				logCallback));

		return serviceCollection;
	}

	public static IServiceBuilder AddLoggingPipelineBehaviour<TRequest, TResponse>(this IServiceBuilder serviceBuilder,
		LogLevel logLevel,
		Action<ILogger, TRequest, TResponse> logCallback) where TRequest : notnull
	{
		if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));
		if (logCallback is null) throw new ArgumentNullException(nameof(logCallback));

		serviceBuilder.DependsOn<ILoggerFactory>();
		serviceBuilder.Services.AddSingleton<IPipelineBehavior<TRequest, TResponse>>(sp =>
			ActivatorUtilities.CreateInstance<LoggingPipelineBehaviour<TRequest, TResponse>>(sp,
				sp.GetRequiredService<ILoggerFactory>().CreateLogger(serviceBuilder.Name),
				logLevel,
				logCallback));

		return serviceBuilder;
	}

	public static IServiceCollection AddSingleThreadedPipelineBehaviour<TRequest, TResponse>(this IServiceCollection serviceCollection,
		Func<TRequest, string> keySelector) where TRequest : notnull
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

		serviceCollection.TryAddSingleton(sp =>
			ActivatorUtilities.CreateInstance<ActorManager>(sp, sp.GetRequiredService<ILogger<ActorManager>>()));
		serviceCollection.AddSingleton<IPipelineBehavior<TRequest, TResponse>>(sp =>
			ActivatorUtilities.CreateInstance<SingleThreadedPipelineBehaviour<TRequest, TResponse>>(sp, keySelector));

		return serviceCollection;
	}

	public static IServiceBuilder AddSingleThreadedPipelineBehaviour<TRequest, TResponse>(this IServiceBuilder serviceBuilder,
		Func<TRequest, string> keySelector) where TRequest : notnull
	{
		if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));
		if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

		serviceBuilder.DependsOn<ILogger<ActorManager>>();
		serviceBuilder.Services.AddSingleThreadedPipelineBehaviour<TRequest, TResponse>(keySelector);

		return serviceBuilder;
	}

	public static IServiceCollection AddCachingPipelineBehaviour<TRequest, TResponse>(this IServiceCollection serviceCollection,
		Func<TRequest, string> cacheKeyGenerator,
		Action<IServiceProvider, ICacheEntry> setCacheTimeout,
		Func<TResponse?, bool> shouldCache) where TRequest : notnull
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (cacheKeyGenerator is null) throw new ArgumentNullException(nameof(cacheKeyGenerator));


		serviceCollection.TryAddSingleton<AsyncSemaphoreFactory>();
		serviceCollection.AddSingleton<IPipelineBehavior<TRequest, TResponse>>(sp =>
			ActivatorUtilities.CreateInstance<CachingPipelineBehaviour<TRequest, TResponse>>(sp, cacheKeyGenerator, setCacheTimeout, shouldCache));

		return serviceCollection;
	}

	public static IServiceBuilder AddCachingPipelineBehaviour<TRequest, TResponse>(this IServiceBuilder serviceBuilder,
		Func<TRequest, string> cacheKeyGenerator,
		Action<IServiceProvider, ICacheEntry> setCacheTimeout,
		Func<TResponse?, bool> shouldCache) where TRequest : notnull
	{
		if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));
		if (cacheKeyGenerator is null) throw new ArgumentNullException(nameof(cacheKeyGenerator));

		serviceBuilder.DependsOn<IMemoryCache>();
		serviceBuilder.DependsOn<ILogger<AsyncSemaphoreFactory>>();
		serviceBuilder.Services.AddCachingPipelineBehaviour(cacheKeyGenerator, setCacheTimeout, shouldCache);

		return serviceBuilder;
	}

	public static IServiceCollection AddCacheInvalidationPipelineBehaviour<TRequest, TResponse>(this IServiceCollection serviceCollection,
		Func<TRequest, string> cacheKeyGenerator) where TRequest : notnull
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (cacheKeyGenerator is null) throw new ArgumentNullException(nameof(cacheKeyGenerator));

		serviceCollection.AddSingleton<IPipelineBehavior<TRequest, TResponse>>(sp =>
			ActivatorUtilities.CreateInstance<CacheInvalidationPipelineBehaviour<TRequest, TResponse>>(sp, cacheKeyGenerator));

		return serviceCollection;
	}

	public static IServiceBuilder AddCacheInvalidationPipelineBehaviour<TRequest, TResponse>(this IServiceBuilder serviceBuilder,
		Func<TRequest, string> cacheKeyGenerator) where TRequest : notnull
	{
		if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));
		if (cacheKeyGenerator is null) throw new ArgumentNullException(nameof(cacheKeyGenerator));

		serviceBuilder.DependsOn<IMemoryCache>();
		serviceBuilder.Services.AddCacheInvalidationPipelineBehaviour<TRequest, TResponse>(cacheKeyGenerator);

		return serviceBuilder;
	}

	public static IServiceCollection AddRetryingPipelineBehaviour<TRequest, TResponse>(this IServiceCollection serviceCollection,
		Func<IServiceProvider, IAsyncPolicy<TResponse>> policySelector) where TRequest : notnull
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
		if (policySelector is null) throw new ArgumentNullException(nameof(policySelector));

		serviceCollection.AddSingleton(policySelector);
		serviceCollection.AddSingleton<IPipelineBehavior<TRequest, TResponse>>(sp =>
			ActivatorUtilities.CreateInstance<RetryingPipelineBehaviour<TRequest, TResponse>>(sp));

		return serviceCollection;
	}

	public static IServiceBuilder AddRetryingPipelineBehaviour<TRequest, TResponse>(this IServiceBuilder serviceBuilder,
		Func<IServiceProvider, IAsyncPolicy<TResponse>> policySelector) where TRequest : notnull
	{
		if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));
		if (policySelector is null) throw new ArgumentNullException(nameof(policySelector));

		serviceBuilder.Services.AddRetryingPipelineBehaviour<TRequest, TResponse>(policySelector);

		return serviceBuilder;
	}
}
