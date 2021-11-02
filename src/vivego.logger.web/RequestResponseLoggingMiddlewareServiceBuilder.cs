using System;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.logger.core;
using vivego.ServiceBuilder;

namespace vivego.logger.web
{
	public sealed class RequestResponseLoggingMiddlewareServiceBuilder : DefaultServiceBuilder<RequestResponseLoggingMiddleware>
	{
		public RequestResponseLoggingMiddlewareServiceBuilder(string name,
			IServiceCollection serviceCollection) : base(name, serviceCollection)
		{
			if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			DependsOn<IMemoryCache>();
			DependsOn<ILogger<TextContentFormatter>>();
			DependsOn<ILogger<RequestResponseLoggingMiddleware>>();
			Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<RequestResponseLoggingMiddleware>(sp, name));
			Services.AddSingleton<IMediaTypeEncodingResolver, MediaTypeEncodingResolver>();
			Services.AddSingleton<IContentFormatter>(sp => ActivatorUtilities.CreateInstance<TextContentFormatter>(sp, 10*1024));
		}
	}
}
