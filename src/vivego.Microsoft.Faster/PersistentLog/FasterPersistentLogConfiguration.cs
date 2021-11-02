using System.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.ServiceBuilder;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Microsoft.Faster.PersistentLog
{
	public static class FasterPersistentLogConfiguration
	{
		public static IServiceBuilder AddFasterPersistentLog(this IServiceCollection serviceCollection,
			FileInfo logDirectory,
			string providerName = "Default",
			bool flushOnStop = true,
			bool deleteOnClose = true)
		{
			IServiceBuilder serviceBuilder = new DefaultServiceBuilder<FasterPersistentLog>(providerName, serviceCollection);

			serviceBuilder.DependsOn<ILogger<FasterPersistentLog>>();
			serviceBuilder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<FasterPersistentLog>(sp,
				providerName,
				logDirectory,
				flushOnStop,
				deleteOnClose,
				sp.GetRequiredService<ILogger<FasterPersistentLog>>()));

			return serviceBuilder;
		}
	}
}
