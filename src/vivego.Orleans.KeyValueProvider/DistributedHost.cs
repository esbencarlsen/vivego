using System;
using System.Net;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Orleans.Configuration;
using Orleans.Hosting;

using vivego.core;
using vivego.Orleans.KeyValueProvider.Membership;
using vivego.Orleans.KeyValueProvider.Storage;

using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace vivego.Orleans.KeyValueProvider
{
	public sealed record DistributedApplicationOptions(string KeyValueStoreName)
	{
		public ClusterOptions? ClusterOptions { get; set; }
		public EndpointOptions? EndpointOptions { get; set; }
	}

	public static class DistributedHostConfigurationExtensions
	{
		private const string DefaultStorageProviderName = "Default";
		public const string PubSubStoreStorageProviderName = "PubSubStore";
		public const string DefaultStreamingProviderName = "SMS";

		public static T ConfigureOrleansDefaults<T>(this T hostBuilder,
			DistributedApplicationOptions? applicationOptions = default,
			Action<HostBuilderContext, ISiloBuilder>? configure = default)
			where T : IHostBuilder
		{
			if (hostBuilder is null) throw new ArgumentNullException(nameof(hostBuilder));

			hostBuilder
				.UseOrleans((context, builder) =>
				{
					applicationOptions ??= new DistributedApplicationOptions("Default");
					builder
						.ConfigureLogging(loggerBuilder => loggerBuilder.AddConsole().SetMinimumLevel(LogLevel.Warning))
						.Configure<ClusterOptions>(options =>
						{
							options.ClusterId = applicationOptions.ClusterOptions?.ClusterId ?? "cid1";
							options.ServiceId = applicationOptions.ClusterOptions?.ServiceId ?? "dc1";
						})
						.Configure<EndpointOptions>(options =>
						{
							options.AdvertisedIPAddress = applicationOptions.EndpointOptions?.AdvertisedIPAddress ?? IPAddress.Loopback;
							options.GatewayListeningEndpoint = applicationOptions.EndpointOptions?.GatewayListeningEndpoint;
							options.SiloListeningEndpoint = applicationOptions.EndpointOptions?.SiloListeningEndpoint;
							options.SiloPort = PortUtils.FindAvailablePortIncrementally(applicationOptions.EndpointOptions?.SiloPort ?? EndpointOptions.DEFAULT_SILO_PORT);
							options.GatewayPort = PortUtils.FindAvailablePortIncrementally(applicationOptions.EndpointOptions?.GatewayPort ?? EndpointOptions.DEFAULT_GATEWAY_PORT);
						})
						.AddKeyValueStoreMembershipProvider(applicationOptions.KeyValueStoreName)
						.AddKeyValueGrainStorage(DefaultStorageProviderName, applicationOptions.KeyValueStoreName, options => options.Ttl = TimeSpan.FromDays(1))
						.AddKeyValueGrainStorage(PubSubStoreStorageProviderName, applicationOptions.KeyValueStoreName, options => options.Ttl = TimeSpan.FromDays(1));
						// EventStore Streaming Provider
						//.AddKeyValueStoreStreams(DefaultStreamingProviderName);
						//.AddGrainStateStreams(DefaultStreamingProviderName);
						//.AddReactiveStateStreams(DefaultStreamingProviderName);
						// .AddMemoryStreams<DefaultMemoryMessageBodySerializer>(DefaultStreamingProviderName,
						// 	configure =>
						// 	{
						// 		configure.ConfigurePartitioning(2);
						// 		// configure
						// 		// 	.ConfigureCacheEviction(optionsBuilder => optionsBuilder.Configure(options =>
						// 		// 	{
						// 		// 		options.DataMaxAgeInCache = TimeSpan.FromMinutes(2);
						// 		// 		options.DataMinTimeInCache = TimeSpan.FromMinutes(1);
						// 		// 	}));
						// 	});

					configure?.Invoke(context, builder);
				});

			return hostBuilder;
		}
	}
}
