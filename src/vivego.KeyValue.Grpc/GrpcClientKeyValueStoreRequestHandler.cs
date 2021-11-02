using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;
using Grpc.Net.Client;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Grpc
{
	public sealed class GrpcClientKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly KeyValueService.KeyValueServiceClient _client;

		public GrpcClientKeyValueStoreRequestHandler(string serverAddress)
		{
			GrpcChannel channel = GrpcChannel.ForAddress(serverAddress);
			_client = new KeyValueService.KeyValueServiceClient(channel);
		}

		public GrpcClientKeyValueStoreRequestHandler(ChannelBase grpcChannel)
		{
			_client = new KeyValueService.KeyValueServiceClient(grpcChannel);
		}

		private KeyValueStoreFeatures? _features;
		public async Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
		{
			if (_features is null)
			{
				GetFeaturesResponse getFeaturesResponse = await _client
					.GetFeaturesAsync(new Empty(), cancellationToken: cancellationToken);
				_features = new KeyValueStoreFeatures
				{
					MaximumDataSize = getFeaturesResponse.MaximumDataSize,
					MaximumKeyLength = getFeaturesResponse.MaximumKeyLength,
					SupportsEtag = getFeaturesResponse.SupportsEtag,
					SupportsTtl = getFeaturesResponse.SupportsTtl
				};
			}

			return _features;
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));
			SetReply setReply = await _client.SetAsync(request.Entry, cancellationToken: cancellationToken);
			return setReply?.ETag ?? string.Empty;
		}

		public async Task<KeyValueEntry> Handle(Get.GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
			KeyValueEntry keyValueEntry = await _client
				.GetAsync(new GetRequest
				{
					Key = request.Key
				}, cancellationToken: cancellationToken);
			return keyValueEntry;
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));
			DeleteResponse keyValueEntry = await _client
				.DeleteAsync(request.Entry, cancellationToken: cancellationToken);
			return keyValueEntry?.Success ?? false;
		}

		public async Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			await _client.ClearAsync(new Empty(), cancellationToken: cancellationToken);
			return Unit.Value;
		}
	}
}
