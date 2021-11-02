using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Grpc;
using vivego.Serializer;

namespace vivego.KeyValue.Tests.Helpers
{
	public sealed class GrpcServerKeyValueStoreUnitTest : IKeyValueStore
	{
		private readonly GrpcServerKeyValueStore _grpcServerKeyValueStore;

		public GrpcServerKeyValueStoreUnitTest()
		{
			HostBuilder hostBuilder = new();
			hostBuilder.ConfigureServices(collection =>
			{
				collection.AddMemoryCache();
				collection.AddNewtonSoftJsonSerializer();
				collection.AddInMemoryKeyValueStore("unittest");
			});
			IHost host = hostBuilder.Build();
			_grpcServerKeyValueStore = new GrpcServerKeyValueStore(host.Services.GetRequiredService<IKeyValueStore>());
		}

		public string Name => "UnitTest";

		public ValueTask<KeyValueStoreFeatures> GetFeatures(CancellationToken cancellationToken = default)
		{
			GetFeaturesResponse getFeaturesResponse = _grpcServerKeyValueStore
				.GetFeatures(new Empty(), TestServerCallContext.Create(cancellationToken: cancellationToken))
				.Result;
			return ValueTask.FromResult(new KeyValueStoreFeatures
			{
				SupportsTtl = getFeaturesResponse.SupportsTtl,
				SupportsEtag = getFeaturesResponse.SupportsEtag,
				MaximumDataSize = getFeaturesResponse.MaximumDataSize,
				MaximumKeyLength = getFeaturesResponse.MaximumKeyLength
			});
		}

		public async ValueTask<string> Set(SetKeyValueEntry setKeyValueEntry, CancellationToken cancellationToken = default)
		{
			SetReply setReply = await _grpcServerKeyValueStore
				.Set(setKeyValueEntry, TestServerCallContext.Create(cancellationToken: cancellationToken))
				.ConfigureAwait(false);
			return setReply.ETag;
		}

		public async ValueTask<KeyValueEntry> Get(string key, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentException(nameof(key));
			KeyValueEntry keyValueEntry = await _grpcServerKeyValueStore
				.Get(new GetRequest
				{
					Key = key
				}, TestServerCallContext.Create(cancellationToken: cancellationToken))
				.ConfigureAwait(false);
			return keyValueEntry;
		}

		public async ValueTask<bool> Delete(DeleteKeyValueEntry deleteKeyValueEntry, CancellationToken cancellationToken = default)
		{
			DeleteResponse deleteResponse = await _grpcServerKeyValueStore
				.Delete(deleteKeyValueEntry, TestServerCallContext.Create(cancellationToken: cancellationToken))
				.ConfigureAwait(false);
			return deleteResponse.Success;
		}

		public ValueTask Clear(CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException();
		}
	}
}
