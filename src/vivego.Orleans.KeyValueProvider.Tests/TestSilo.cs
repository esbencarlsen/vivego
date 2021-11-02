using System;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

using vivego.core;
using vivego.KeyValue;
using vivego.Orleans.KeyValueProvider.Membership;
using vivego.Orleans.KeyValueProvider.Storage;
using vivego.Serializer;

using Xunit.Abstractions;

namespace vivego.Orleans.KeyValueProvider.Tests
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
					collection.AddInMemoryKeyValueStore("UnitTest");
					collection.AddNewtonSoftJsonSerializer();
				});
			}
		}
	}

	public sealed class TestSilo : DisposableBase
	{
		public TestCluster Cluster { get; }

		public ITestOutputHelper? TestOutputHelper { get; set; }

		public TestSilo()
		{
			TestClusterBuilder builder = new(1);
			builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
			Cluster = builder.Build();
			Cluster.Deploy();
		}

		protected override void Cleanup()
		{
			Cluster.StopAllSilos();
			Cluster.Dispose();
		}

		private class TestSiloConfigurator : ISiloConfigurator
		{
			public void Configure(ISiloBuilder hostBuilder)
			{
				hostBuilder
					.AddKeyValueGrainStorage("Default", provider => provider.GetRequiredService<IKeyValueStore>(), options => options.Ttl = TimeSpan.FromDays(365))
					.AddKeyValueStoreMembershipProvider(provider => provider.GetRequiredService<IKeyValueStore>())
					.ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(TestGrain).Assembly).WithReferences())
					.ConfigureServices(services =>
					{
						services.AddSingleton(_ => InstanceRepository.KeyValueStore);
					});
			}
		}
	}
}
