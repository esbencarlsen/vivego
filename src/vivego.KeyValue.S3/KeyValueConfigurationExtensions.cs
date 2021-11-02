using System;

using Amazon;
using Amazon.S3;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.KeyValue.S3;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddS3KeyValueStore(this IServiceCollection collection,
			string name,
			AmazonS3Options amazonS3Options)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (amazonS3Options is null) throw new ArgumentNullException(nameof(amazonS3Options));
			if (amazonS3Options.BucketName is null) throw new ArgumentNullException(nameof(amazonS3Options.BucketName));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.RegisterKeyValueStoreRequestHandler<S3KeyValueStoreRequestHandler>();
			builder.Services.AddSingleton(sp =>
			{
				AmazonS3Config amazonS3Config = new()
				{
					RegionEndpoint = RegionEndpoint.GetBySystemName(amazonS3Options.RegionEndpoint),
					ServiceURL = amazonS3Options.Endpoint,
					ForcePathStyle = true // MUST be true to work correctly with MinIO server
				};

				AmazonS3Client amazonS3Client = new(amazonS3Options.AccessKey,
					amazonS3Options.SecretKey,
					amazonS3Config);

				return ActivatorUtilities.CreateInstance<S3KeyValueStoreRequestHandler>(sp,
					sp.GetRequiredService<ILoggerFactory>().CreateLogger<S3KeyValueStoreRequestHandler>(),
					amazonS3Client,
					amazonS3Options.BucketName);

			});
			return builder;
		}
	}
}