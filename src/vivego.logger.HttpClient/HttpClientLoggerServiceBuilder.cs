using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.logger.core;
using vivego.ServiceBuilder;

namespace vivego.logger.HttpClient
{
	public sealed class HttpClientLoggerServiceBuilder : DefaultServiceBuilder<LoggingDelegatingHandler>
	{
		public HttpClientLoggerServiceBuilder(
			string name,
			LogLevel logLevel,
#pragma warning disable CA1062
			IHttpClientBuilder httpClientBuilder) : base(name, httpClientBuilder.Services!)
#pragma warning restore CA1062
		{
			httpClientBuilder.AddHttpMessageHandler<LoggingDelegatingHandler>();
			Services.AddTransient(sp => ActivatorUtilities.CreateInstance<LoggingDelegatingHandler>(sp, name));

			DependsOn<ILogger<DefaultRequestResponseLogger>>();
			Services.AddSingleton<IRequestResponseLogger, DefaultRequestResponseLogger>();

			DependsOn<IMemoryCache>();
			Services.AddSingleton<IMediaTypeEncodingResolver, MediaTypeEncodingResolver>();

			DependsOn<ILogger<TextContentFormatter>>();
			Services.AddSingleton<IContentFormatter>(sp => ActivatorUtilities.CreateInstance<TextContentFormatter>(sp, 10 * 1024));

			Services.AddOptions<ResponseLoggerRequestHandlerOptions>();
			Services.PostConfigure<ResponseLoggerRequestHandlerOptions>(options => options.Level = logLevel);
		}
	}
}
