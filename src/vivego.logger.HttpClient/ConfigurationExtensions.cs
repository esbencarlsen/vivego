using System;
using System.Net.Http;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.logger.HttpClient
{
	public static class ConfigurationExtensions
	{
		public static IServiceBuilder AddDefaultLoggingHandler(this IHttpClientBuilder httpClientBuilder,
			LogLevel logLevel = LogLevel.Debug)
		{
			if (httpClientBuilder is null) throw new ArgumentNullException(nameof(httpClientBuilder));

			IServiceBuilder serviceBuilder = new HttpClientLoggerServiceBuilder(Guid.NewGuid().ToString(),
				logLevel,
				httpClientBuilder);
			serviceBuilder.Services.AddSingleton<DefaultRequestResponseLoggerRequestHandler>();
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogHttpRequestResponseRequest, Unit>>(sp => sp.GetRequiredService<DefaultRequestResponseLoggerRequestHandler>());
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogHttpRequestExceptionRequest, Unit>>(sp => sp.GetRequiredService<DefaultRequestResponseLoggerRequestHandler>());

			return serviceBuilder;
		}

		public static IServiceBuilder AddSimpleLoggingHandler(this IHttpClientBuilder httpClientBuilder,
			LogLevel logLevel = LogLevel.Debug)
		{
			if (httpClientBuilder is null) throw new ArgumentNullException(nameof(httpClientBuilder));

			IServiceBuilder serviceBuilder = new HttpClientLoggerServiceBuilder(Guid.NewGuid().ToString(),
				logLevel,
				httpClientBuilder);
			serviceBuilder.Services.TryAddSingleton<SimpleRequestResponseLogger>();
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogHttpRequestResponseRequest, Unit>>(sp => sp.GetRequiredService<SimpleRequestResponseLogger>());
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogHttpRequestExceptionRequest, Unit>>(sp => sp.GetRequiredService<SimpleRequestResponseLogger>());

			return serviceBuilder;
		}

		public static IServiceBuilder AddHttpProtocolRequestHandler(this IHttpClientBuilder httpClientBuilder,
			LogLevel logLevel = LogLevel.Debug)
		{
			if (httpClientBuilder is null) throw new ArgumentNullException(nameof(httpClientBuilder));

			IServiceBuilder serviceBuilder = new HttpClientLoggerServiceBuilder(Guid.NewGuid().ToString(),
				logLevel,
				httpClientBuilder);
			serviceBuilder.Services.TryAddSingleton<HttpProtocolRequestLoggerRequestHandler>();
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogHttpRequestResponseRequest, Unit>>(sp => sp.GetRequiredService<HttpProtocolRequestLoggerRequestHandler>());
			serviceBuilder.Services.AddSingleton<IRequestHandler<LogHttpRequestExceptionRequest, Unit>>(sp => sp.GetRequiredService<HttpProtocolRequestLoggerRequestHandler>());

			return serviceBuilder;
		}

		public static IServiceBuilder AddDictionaryScopedRequestResponseBehaviour(this IServiceBuilder serviceBuilder)
		{
			if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));

			serviceBuilder.Services.AddSingleton<IPipelineBehavior<LogHttpRequestResponseRequest, Unit>, DictionaryScopedRequestResponseBehaviour>();

			return serviceBuilder;
		}

		public static IServiceBuilder AddFilteringLambdaRequestResponseBehaviour(this IServiceBuilder serviceBuilder,
			Func<HttpRequestMessage, HttpResponseMessage, TimeSpan, Task<bool>> lambdaFilter)
		{
			if (serviceBuilder is null) throw new ArgumentNullException(nameof(serviceBuilder));

			serviceBuilder.Services.AddSingleton<IPipelineBehavior<LogHttpRequestResponseRequest, Unit>>(_ =>
				new FilteringLambdaRequestResponseBehaviour(lambdaFilter));

			return serviceBuilder;
		}
	}
}