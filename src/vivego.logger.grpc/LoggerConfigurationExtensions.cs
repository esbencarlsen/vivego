using System;

using Microsoft.Extensions.DependencyInjection;

namespace vivego.logger.grpc
{
	public static class LoggerConfigurationExtensions
	{
		public static IServiceCollection AddGrpcLogger(this IServiceCollection collection)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			collection.AddTransient<LoggerInterceptor>();
			return collection;
		}
	}
}