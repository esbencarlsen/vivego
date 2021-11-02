using System;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.logger.web
{
	public static class ConfigurationExtensions
	{
		public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder is null) throw new ArgumentNullException(nameof(applicationBuilder));

			applicationBuilder.UseMiddleware<RequestResponseLoggingMiddleware>();

			return applicationBuilder;
		}

		public static IServiceBuilder AddLoggingMiddleware(this IServiceCollection serviceCollection,
			string? providerName = default)
		{
			if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

			IServiceBuilder builder = new RequestResponseLoggingMiddlewareServiceBuilder(providerName ?? "Default", serviceCollection);

			return builder;
		}

		public static IServiceBuilder AddLoggingHandler(this IServiceBuilder serviceBuilder,
			LogLevel logLevel = LogLevel.Debug,
			Predicate<LogRequest>? predicate = default)
		{
			if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));

			serviceBuilder.Services.AddOptions<DefaultRequestResponseHandlerOptions>();
			serviceBuilder.Services.PostConfigure<DefaultRequestResponseHandlerOptions>(options =>
			{
				options.LogLevel = logLevel;
				options.Predicate = predicate;
			});

			serviceBuilder.Services.AddSingleton<DefaultRequestResponseHandler>();
			serviceBuilder.Services.AddSingleton<IRequestHandler<GetLogRequestOptionsRequest, RecordOptions>>(sp => sp.GetRequiredService<DefaultRequestResponseHandler>());
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogRequest, Unit>>(sp => sp.GetRequiredService<DefaultRequestResponseHandler>());

			return serviceBuilder;
		}

		public static IServiceBuilder AddDictionaryScopedLoggerPipelineHandler(this IServiceBuilder serviceBuilder)
		{
			if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));

			serviceBuilder.Services.AddSingleton<IPipelineBehavior<LogRequest, Unit>, DictionaryScopedLoggerPipelineBehaviour>();

			return serviceBuilder;
		}
	}
}
