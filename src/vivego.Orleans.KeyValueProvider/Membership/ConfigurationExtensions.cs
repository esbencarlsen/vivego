using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Orleans;
using Orleans.Hosting;
using Orleans.Messaging;

using vivego.KeyValue;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Orleans.KeyValueProvider.Membership
{
	public static class ConfigurationExtensions
	{
		public static ISiloBuilder AddKeyValueStoreMembershipProvider(this ISiloBuilder siloBuilder,
			Func<IServiceProvider, IKeyValueStore>? keyValueFactory = default)
		{
			return siloBuilder
				.ConfigureServices(services =>
				{
					services.AddKeyValueStoreMembershipProvider(keyValueFactory);
				})
				.ConfigureApplicationParts(manager =>
					manager.AddApplicationPart(typeof(KeyValueStoreMembershipTable).Assembly).WithReferences());
		}

		public static ISiloBuilder AddKeyValueStoreMembershipProvider(this ISiloBuilder siloBuilder,
			string keyValueStoreName)
		{
			if (string.IsNullOrEmpty(keyValueStoreName)) throw new ArgumentException("Value cannot be null or empty.", nameof(keyValueStoreName));
			return siloBuilder
				.AddKeyValueStoreMembershipProvider(sp => sp.GetRequiredService<IServiceManager<IKeyValueStore>>().Get(keyValueStoreName));
		}

		public static IServiceCollection AddKeyValueStoreMembershipProvider(this IServiceCollection services,
			string keyValueStoreName)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));
			if (string.IsNullOrEmpty(keyValueStoreName)) throw new ArgumentException("Value cannot be null or empty.", nameof(keyValueStoreName));
			return services
				.AddKeyValueStoreMembershipProvider(sp => sp.GetRequiredService<IServiceManager<IKeyValueStore>>().Get(keyValueStoreName));
		}

		public static IServiceCollection AddKeyValueStoreMembershipProvider(this IServiceCollection services,
			Func<IServiceProvider, IKeyValueStore>? keyValueFactory = default)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));
			services.TryAddSingleton(sp =>
			{
				IKeyValueStore keyValueStore = keyValueFactory is null
					? sp.GetRequiredService<IKeyValueStore>()
					: keyValueFactory(sp);
				return ActivatorUtilities.CreateInstance<KeyValueStoreMembershipTable>(sp, keyValueStore);
			});
			services.TryAddSingleton<IMembershipTable>(sp => sp.GetRequiredService<KeyValueStoreMembershipTable>());
			services.TryAddSingleton<IGatewayListProvider>(sp => sp.GetRequiredService<KeyValueStoreMembershipTable>());
			return services;
		}

		public static IClientBuilder AddKeyValueStoreMembershipProvider(this IClientBuilder clientBuilder,
			Func<IServiceProvider, IKeyValueStore>? keyValueFactory = default)
		{
			if (clientBuilder is null) throw new ArgumentNullException(nameof(clientBuilder));
			return clientBuilder
				.ConfigureServices(services =>
				{
					services.AddKeyValueStoreMembershipProvider(keyValueFactory);
				})
				.ConfigureApplicationParts(manager =>
					manager.AddApplicationPart(typeof(KeyValueStoreMembershipTable).Assembly).WithReferences());
		}
	}
}
