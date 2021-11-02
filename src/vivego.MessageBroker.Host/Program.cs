using System;
using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

using vivego.core;
using vivego.KeyValue;
using vivego.MessageBroker.EventStore;
using vivego.MessageBroker.Host;
using vivego.MessageBroker.PublishSubscribe;
using vivego.Orleans.KeyValueProvider.Membership;
using vivego.Orleans.KeyValueProvider.Storage;

using Host = Microsoft.Extensions.Hosting.Host;

#pragma warning disable CA1812
Host.CreateDefaultBuilder(args)
	.UseOrleans((context, builder) =>
	{
		builder
			.Configure<ClusterOptions>(context.Configuration.GetSection("ClusterOptions"))
			.Configure<EndpointOptions>(options =>
			{
				IConfigurationSection endpointOptions = context.Configuration.GetSection("EndpointOptions");

				int siloPort = endpointOptions.GetValue("SiloPort", EndpointOptions.DEFAULT_SILO_PORT);
				options.SiloPort = PortUtils.FindAvailablePortIncrementally(siloPort);

				int gatewayPort = endpointOptions.GetValue("GatewayPort", EndpointOptions.DEFAULT_SILO_PORT);
				options.GatewayPort = PortUtils.FindAvailablePortIncrementally(gatewayPort);

				string? advertisedIpAddressString = endpointOptions.GetValue<string>("AdvertisedIPAddress");
				if (!string.IsNullOrEmpty(advertisedIpAddressString)
					&& IPAddress.TryParse(advertisedIpAddressString, out IPAddress? advertisedIpAddress))
				{
					options.AdvertisedIPAddress = advertisedIpAddress;
				}

				string? gatewayListeningEndpointString = endpointOptions.GetValue<string>("GatewayListeningEndpoint");
				if (!string.IsNullOrEmpty(gatewayListeningEndpointString)
					&& IPEndPoint.TryParse(gatewayListeningEndpointString, out IPEndPoint? gatewayListeningEndpoint))
				{
					options.GatewayListeningEndpoint = gatewayListeningEndpoint;
				}

				string? siloListeningEndpointString = endpointOptions.GetValue<string>("SiloListeningEndpoint");
				if (!string.IsNullOrEmpty(siloListeningEndpointString)
					&& IPEndPoint.TryParse(siloListeningEndpointString, out IPEndPoint? siloListeningEndpoint))
				{
					options.SiloListeningEndpoint = siloListeningEndpoint;
				}
			})
			.ConfigureServices(collection =>
			{
				// collection
				// 	.AddCassandraKeyValueStore("Default", "Contact Points=localhost;Port=9042;Default Keyspace=deleteme", false, "deleteme", ConsistencyLevel.LocalQuorum, false)
				// 	.AddCachingPipelineBehaviour<GetRequest, KeyValueEntry>(
				// 		request => nameof(GetRequest) + request.Key,
				// 		(_, entry) => entry.SlidingExpiration = TimeSpan.FromMinutes(1),
				// 		entry => entry is not null);
				//collection.AddInMemoryKeyValueStore("Default");
				collection
					.AddRedisKeyValueStore("Default", "localhost", "grainstate", true)
					.AddCachingKeyValueStoreBehaviour((_, entry) => entry.SlidingExpiration = TimeSpan.FromMinutes(1));

				// collection
				// 	.AddCassandraKeyValueStore("Membership", "Contact Points=localhost;Port=9042;Default Keyspace=deleteme", false, "deleteme", ConsistencyLevel.LocalQuorum, true);
				collection.AddRedisKeyValueStore("Membership", "localhost", "Membership", false);
			})
			.AddKeyValueStoreMembershipProvider("Membership")
			.AddKeyValueGrainStorage("Default", "Default", options => options.Ttl = TimeSpan.FromDays(1))
			.EventStoreConfigureApplicationParts()
			.PublishSubscribeConfigureApplicationParts()
			.UseDashboard(options => options.Port = 10000);
	})
	.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
	.Build()
	.Run();
