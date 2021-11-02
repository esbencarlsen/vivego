using System;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;
using Orleans.TestingHost;

using vivego.Collection.EventStore;
using vivego.Collection.Orleans.EventStore;
using vivego.core;
using vivego.KeyValue;
using vivego.Serializer;

using Xunit;

namespace vivego.Collection.Tests.EventStore
{
	public sealed class ClusterFixture : DisposableBase
	{
		internal class SiloConfigurator : ISiloConfigurator
		{
			public void Configure(ISiloBuilder hostBuilder)
			{
				hostBuilder
					.ConfigureServices(collection =>
					{
						collection.AddMemoryCache();
						collection.AddInMemoryKeyValueStore("Default");
						collection
							.AddEventStore()
							.AddOrleansSingleThreadAccessPipelineBehavior();
						collection.AddLogging();
						collection.AddSystemJsonSerializer();
					});
			}
		}

		public ClusterFixture()
		{
			Cluster = new TestClusterBuilder(1)
				.AddSiloBuilderConfigurator<SiloConfigurator>()
				.Build();
			Cluster.Deploy();
			RegisterDisposable(() => Cluster.StopAllSilos());
		}

		public TestCluster Cluster { get; }
	}

	[CollectionDefinition(Name)]
	public sealed class ClusterCollection : ICollectionFixture<ClusterFixture>
	{
		public const string Name = "ClusterCollection";
	}

	[Collection(ClusterCollection.Name)]
	public sealed class OrleansEventStoreTests : EventStoreTests
	{
		private readonly ClusterFixture _fixture;

		public OrleansEventStoreTests(ClusterFixture fixture) =>
			_fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

		protected override IEventStore MakeEventStore()
		{
			return ((InProcessSiloHandle)_fixture.Cluster.Primary).SiloHost.Services.GetRequiredService<IEventStore>();
		}
	}
}
