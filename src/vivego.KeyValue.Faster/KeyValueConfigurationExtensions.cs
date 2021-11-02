using System;
using System.Globalization;
using System.IO;
using System.Linq;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using vivego.KeyValue.Delete;
using vivego.KeyValue.Faster;
using vivego.KeyValue.Features;
using vivego.KeyValue.Set;
using vivego.Microsoft.Faster.PersistentLog;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddFasterPipelineBehavior(this IServiceBuilder builder,
			int transactionLogWorkerThreadCount,
			bool flushOnStop = true,
			bool deleteOnClose = true)
		{
			if (builder is null) throw new ArgumentNullException(nameof(builder));
			string transactionLogFileName = Path.Combine(Directory.GetCurrentDirectory(), builder.Name, "log");
			DirectoryInfo logRoot = new(transactionLogFileName);
			return builder.AddFasterPipelineBehavior(transactionLogWorkerThreadCount, logRoot, flushOnStop, deleteOnClose);
		}

		public static IServiceBuilder AddFasterPipelineBehavior(this IServiceBuilder builder,
			int transactionLogWorkerThreadCount,
			DirectoryInfo logRoot,
			bool flushOnStop = true,
			bool deleteOnClose = true)
		{
			if (transactionLogWorkerThreadCount <= 0) throw new ArgumentOutOfRangeException(nameof(transactionLogWorkerThreadCount));
			if (transactionLogWorkerThreadCount > 100) throw new ArgumentOutOfRangeException(nameof(transactionLogWorkerThreadCount));
			if (builder is null) throw new ArgumentNullException(nameof(builder));
			if (logRoot is null) throw new ArgumentNullException(nameof(logRoot));

			builder.DependsOn<ILogger<FasterPersistentLog>>();
			foreach (int i in Enumerable.Range(0, transactionLogWorkerThreadCount))
			{
				builder.Services.AddFasterPersistentLog(
					new FileInfo(logRoot.CreateSubdirectory(i.ToString(CultureInfo.InvariantCulture)).FullName + "\\log"),
					$"{logRoot.Name}_{i}",
					flushOnStop,
					deleteOnClose);
			}

			builder.DependsOn<ILogger<FasterKeyValueStorePipelineBehavior>>();
			builder.Services.AddSingleton<FasterKeyValueStorePipelineBehavior>();
			builder.Services.AddSingleton<IPipelineBehavior<FeaturesRequest, KeyValueStoreFeatures>>(sp => sp.GetRequiredService<FasterKeyValueStorePipelineBehavior>());
			builder.Services.AddSingleton<IPipelineBehavior<SetRequest, string>>(sp => sp.GetRequiredService<FasterKeyValueStorePipelineBehavior>());
			builder.Services.AddSingleton<IPipelineBehavior<DeleteRequest, bool>>(sp => sp.GetRequiredService<FasterKeyValueStorePipelineBehavior>());
			builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<FasterKeyValueStorePipelineBehavior>());
			builder.Map<IHostedService>();

			return builder;
		}
	}
}
