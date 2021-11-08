using System;
using System.Linq;
using System.Threading.Tasks;

using MediatR;
using MediatR.Pipeline;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

using vivego.core;
using vivego.KeyValue;
using vivego.MessageBroker.EventStore;
using vivego.MessageBroker.MessageBroker;
using vivego.MessageBroker.PublishSubscribe;
using vivego.Orleans.KeyValueProvider.Membership;
using vivego.Orleans.KeyValueProvider.Storage;

using Xunit.Abstractions;

namespace vivego.MessageBroker.Tests
{
	public static class InstanceRepository
	{
		private static IKeyValueStore? s_keyValueStore;

		public static IKeyValueStore KeyValueStore
		{
			get
			{
				return s_keyValueStore ??= KeyValueStoreBuilder.MakeKeyValueStore(collection =>
				{
					collection.AddMemoryCache();
					collection.AddLogging();
					collection.AddInMemoryKeyValueStore("Default");
				});
			}
		}
	}

	public sealed class TestSilo : AsyncDisposableBase
	{
		public TestCluster Cluster { get; }

		public ITestOutputHelper? TestOutputHelper { get; set; }

		public TestSilo()
		{
			TestClusterBuilder builder = new(1);
			builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
			builder.AddClientBuilderConfigurator<TestSiloClientBuilderConfigurator>();
			Cluster = builder.Build();
			Cluster.Deploy();
		}

		protected override async Task Cleanup()
		{
			await Cluster.StopAllSilosAsync().ConfigureAwait(false);
			await Cluster.DisposeAsync().ConfigureAwait(false);
		}

		private class TestSiloClientBuilderConfigurator : IClientBuilderConfigurator
		{
			public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
			{
				clientBuilder.ConfigureServices(collection =>
				{
					collection.TryAddPublishSubscribe();
				});
			}
		}

		private class TestSiloConfigurator : ISiloConfigurator
		{
			public void Configure(ISiloBuilder hostBuilder)
			{
				hostBuilder
					.AddKeyValueGrainStorage("Default",
						provider => provider.GetRequiredService<IKeyValueStore>(),
						options => options.Ttl = TimeSpan.FromDays(365))
					.AddKeyValueStoreMembershipProvider(provider => provider.GetRequiredService<IKeyValueStore>())
					.ConfigureServices(services =>
					{
						services.AddMemoryCache();
						services
							.AddInMemoryKeyValueStore("Default")
							.Services
							.AddSingleton(InstanceRepository.KeyValueStore);

						services.TryAddMessageBroker(hostBuilder.GetConfiguration());

						services.AddSingleton<IMediator, Mediator>();
						services.AddSingleton(p => new ServiceFactory(p.GetService!));
					})
					.EventStoreConfigureApplicationParts()
					.PublishSubscribeConfigureApplicationParts();
			}
		}

		public IServiceProvider SileServiceProvider => Cluster.Silos
			.OfType<InProcessSiloHandle>()
			.First()
			.SiloHost
			.Services;
	}
}
