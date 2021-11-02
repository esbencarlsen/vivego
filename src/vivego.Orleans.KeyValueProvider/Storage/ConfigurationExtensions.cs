using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Storage;

using vivego.KeyValue;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Orleans.KeyValueProvider.Storage
{
	public static class ConfigurationExtensions
	{
		public static ISiloBuilder AddKeyValueGrainStorage(
			this ISiloBuilder builder,
			string providerName,
			Func<IServiceProvider, IKeyValueStore>? keyValueStoreFactory = default,
			Action<KeyValueStoreStorageOptions>? configureOptions = default)
		{
			if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));

			return builder
				.ConfigureServices(services => services
					.AddKeyValueGrainStorage(providerName, keyValueStoreFactory, optionsBuilder =>
					{
						if (configureOptions is not null)
						{
							optionsBuilder.Configure(configureOptions);
						}
					}));
		}

		public static ISiloBuilder AddKeyValueGrainStorage(
			this ISiloBuilder builder,
			string providerName,
			string keyValueStoreName,
			Action<KeyValueStoreStorageOptions>? configureOptions = default)
		{
			if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));
			if (string.IsNullOrEmpty(keyValueStoreName)) throw new ArgumentException("Value cannot be null or empty.", nameof(keyValueStoreName));

			return builder
				.AddKeyValueGrainStorage(providerName,
					sp => sp.GetRequiredService<IServiceManager<IKeyValueStore>>().Get(keyValueStoreName),
					configureOptions);
		}

		public static IServiceCollection AddKeyValueGrainStorage(
			this IServiceCollection services,
			string providerName,
			string keyValueStoreName,
			Action<OptionsBuilder<KeyValueStoreStorageOptions>>? configureOptions = default)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));
			if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));
			if (string.IsNullOrEmpty(keyValueStoreName)) throw new ArgumentException("Value cannot be null or empty.", nameof(keyValueStoreName));

			return services.AddKeyValueGrainStorage(providerName,
				sp => sp.GetRequiredService<IServiceManager<IKeyValueStore>>().Get(keyValueStoreName),
				configureOptions);
		}

		public static IServiceCollection AddKeyValueGrainStorage(
			this IServiceCollection services,
			string providerName,
			Func<IServiceProvider, IKeyValueStore>? keyValueStoreFactory = default,
			Action<OptionsBuilder<KeyValueStoreStorageOptions>>? configureOptions = default)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));
			if (string.IsNullOrEmpty(providerName)) throw new ArgumentException("Value cannot be null or empty.", nameof(providerName));

			OptionsBuilder<KeyValueStoreStorageOptions> optionsBuilder = services.AddOptions<KeyValueStoreStorageOptions>(providerName);
			configureOptions?.Invoke(optionsBuilder);

			services.AddTransient<IConfigurationValidator>(sp => new KeyValueStoreGrainStorageOptionsValidator(
				sp.GetRequiredService<IOptionsMonitor<KeyValueStoreStorageOptions>>().Get(providerName), providerName));
			services.ConfigureNamedOptionForLogging<KeyValueStoreStorageOptions>(providerName);

			if (string.Equals(providerName, "Default", StringComparison.Ordinal))
			{
				services.AddSingleton(sp => sp.GetRequiredServiceByName<IGrainStorage>(providerName));
			}

			return services.AddSingletonNamedService(providerName, (sp, n) =>
				KeyValueStoreGrainStorage.Create(sp, n, keyValueStoreFactory));
		}
	}
}