using System;
using System.Threading.Tasks;

using Orleans.TestingHost;

using Xunit;
using Xunit.Abstractions;

namespace vivego.Orleans.KeyValueProvider.Tests
{
	public sealed class KeyValueStoreGrainStorageTests : IClassFixture<TestSilo>
	{
		private readonly TestSilo _testSilo;

		public KeyValueStoreGrainStorageTests(TestSilo testSilo, ITestOutputHelper testOutputHelper)
		{
			_testSilo = testSilo ?? throw new ArgumentNullException(nameof(testSilo));
			_testSilo.TestOutputHelper = testOutputHelper;
		}

		[Fact]
		public async Task CanWriteReadState()
		{
			string key = Guid.NewGuid().ToString();
			ITestGrain testGrain = _testSilo.Cluster.Client.GetGrain<ITestGrain>(key);

			string value = Guid.NewGuid().ToString();
			await testGrain.Set(value).ConfigureAwait(false);

			foreach (SiloHandle siloHandle in _testSilo.Cluster.Silos)
			{
				await _testSilo.Cluster.RestartSiloAsync(siloHandle).ConfigureAwait(false);
			}

			_testSilo.TestOutputHelper?.WriteLine(_testSilo.Cluster.GetLog());

			testGrain = _testSilo.Cluster.Client.GetGrain<ITestGrain>(key);
			string retrievedValue = await testGrain.Get().ConfigureAwait(false);
			Assert.NotNull(retrievedValue);
			Assert.Equal(value, retrievedValue);
		}

		[Fact]
		public async Task CanClearState()
		{
			string key = Guid.NewGuid().ToString();
			ITestGrain testGrain = _testSilo.Cluster.Client.GetGrain<ITestGrain>(key);

			string value = Guid.NewGuid().ToString();
			await testGrain.Set(value).ConfigureAwait(false);
			await testGrain.Clear().ConfigureAwait(false);

			foreach (SiloHandle siloHandle in _testSilo.Cluster.Silos)
			{
				await _testSilo.Cluster.RestartSiloAsync(siloHandle).ConfigureAwait(false);
			}

			_testSilo.TestOutputHelper?.WriteLine(_testSilo.Cluster.GetLog());

			testGrain = _testSilo.Cluster.Client.GetGrain<ITestGrain>(key);
			string retrievedValue = await testGrain.Get().ConfigureAwait(false);
			Assert.Equal(string.Empty, retrievedValue);
		}
	}
}
