using System;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue.Grpc
{
	public sealed class GrpcServerKeyValueStore : KeyValueService.KeyValueServiceBase
	{
		private readonly IKeyValueStore _keyValueStore;

		public GrpcServerKeyValueStore(IKeyValueStore keyValueStore) => _keyValueStore =
			keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));

		public override async Task<GetFeaturesResponse> GetFeatures(Empty request, ServerCallContext context)
		{
			if (context is null) throw new ArgumentNullException(nameof(context));
			KeyValueStoreFeatures keyValueStoreFeatures = await _keyValueStore
				.GetFeatures(context.CancellationToken)
				.ConfigureAwait(false);
			return new GetFeaturesResponse
			{
				MaximumDataSize = keyValueStoreFeatures.MaximumDataSize,
				MaximumKeyLength = keyValueStoreFeatures.MaximumKeyLength,
				SupportsEtag = keyValueStoreFeatures.SupportsEtag,
				SupportsTtl = keyValueStoreFeatures.SupportsTtl
			};
		}

		public override async Task<SetReply> Set(SetKeyValueEntry setKeyValueEntry, ServerCallContext context)
		{
			if (setKeyValueEntry is null) throw new ArgumentNullException(nameof(setKeyValueEntry));
			if (context is null) throw new ArgumentNullException(nameof(context));
			if (string.IsNullOrEmpty(setKeyValueEntry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(setKeyValueEntry.Key));
			string etag = await _keyValueStore
				.Set(setKeyValueEntry, context.CancellationToken)
				.ConfigureAwait(false);
			return new SetReply
			{
				ETag = etag
			};
		}

		public override async Task<KeyValueEntry> Get(GetRequest request, ServerCallContext context)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (context is null) throw new ArgumentNullException(nameof(context));
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
			return await _keyValueStore.Get(request.Key, context.CancellationToken).ConfigureAwait(false);
		}

		public override async Task<DeleteResponse> Delete(DeleteKeyValueEntry request, ServerCallContext context)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
			if (context is null) throw new ArgumentNullException(nameof(context));
			bool success = await _keyValueStore
				.Delete(request, context.CancellationToken)
				.ConfigureAwait(false);
			return new DeleteResponse
			{
				Success = success
			};
		}
	}
}
