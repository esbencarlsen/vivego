using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

using Google.Protobuf;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.S3
{
	public sealed class S3KeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly IAmazonS3 _amazonS3;
		private readonly string _bucketName;
		private readonly Task<KeyValueStoreFeatures> _features;

		public S3KeyValueStoreRequestHandler(
			IAmazonS3 amazonS3,
			string bucketName)
		{
			if (string.IsNullOrEmpty(bucketName)) throw new ArgumentException("Value cannot be null or empty.", nameof(bucketName));

			_amazonS3 = amazonS3 ?? throw new ArgumentNullException(nameof(amazonS3));
			_bucketName = bucketName;

			_features = Task.FromResult(new KeyValueStoreFeatures
			{
				SupportsEtag = false,
				SupportsTtl = false,
				MaximumDataSize = 1024L * 1024L * 1024L * 1024L * 5L, // 5TB
				MaximumKeyLength = 1024
			});
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
		{
			return _features;
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry();

			MemoryStream memoryStream = new(keyValueEntry.CalculateSize());
			await using ConfiguredAsyncDisposable _ = memoryStream.ConfigureAwait(false);

			keyValueEntry.WriteTo(memoryStream);
			PutObjectRequest putObjectRequest = new()
			{
				Key = KeyHelper.Instance.MakeValidKey(request.Entry.Key),
				BucketName = _bucketName,
				AutoCloseStream = true,
				AutoResetStreamPosition = true,
				InputStream = memoryStream,
				ContentType = "application/binary"
			};

			await _amazonS3
				.PutObjectAsync(putObjectRequest, cancellationToken)
				.ConfigureAwait(false);

			return keyValueEntry.ETag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
			try
			{
				using GetObjectResponse getObjectResponse = await _amazonS3
					.GetObjectAsync(_bucketName, KeyHelper.Instance.MakeValidKey(request.Key), cancellationToken)
					.ConfigureAwait(false);
				return KeyValueEntry.Parser.ParseFrom(getObjectResponse.ResponseStream);
			}
			catch (AmazonS3Exception amazonS3Exception) when (amazonS3Exception.StatusCode == HttpStatusCode.NotFound)
			{
			}

			return new KeyValueEntry();
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			try
			{
				DeleteObjectResponse deleteObjectResponse = await _amazonS3
					.DeleteObjectAsync(_bucketName, KeyHelper.Instance.MakeValidKey(request.Entry.Key), cancellationToken)
					.ConfigureAwait(false);

				return deleteObjectResponse.HttpStatusCode == HttpStatusCode.NoContent;
			}
			catch (AmazonS3Exception amazonS3Exception) when (amazonS3Exception.StatusCode == HttpStatusCode.NotFound)
			{
			}

			return false;
		}

		public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
	}
}
