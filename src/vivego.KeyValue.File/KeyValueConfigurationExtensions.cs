using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using vivego.KeyValue.File;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddFileKeyValueStore(this IServiceCollection collection,
			string name,
			string path)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (string.IsNullOrEmpty(path)) throw new ArgumentException("Value cannot be null or empty.", nameof(path));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.DependsOn<ILogger<FileKeyValueStoreRequestHandler>>();
			builder.AddIsolatedKeyValueStoreBehaviour();
			builder.RegisterKeyValueStoreRequestHandler<FileKeyValueStoreRequestHandler>();
			builder.Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<FileKeyValueStoreRequestHandler>(sp, path));

			return builder;
		}
	}
}