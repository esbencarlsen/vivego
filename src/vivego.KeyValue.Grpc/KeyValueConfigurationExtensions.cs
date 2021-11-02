using System;

using Grpc.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using vivego.KeyValue.Grpc;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static KeyValueStoreBuilder AddGrpcClientKeyValueStore(this IServiceCollection collection,
			string name,
			string serverAddress)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.RegisterKeyValueStoreRequestHandler<GrpcClientKeyValueStoreRequestHandler>();
			builder.Services.TryAddSingleton(sp => ActivatorUtilities.CreateInstance<GrpcClientKeyValueStoreRequestHandler>(sp, serverAddress));
			return builder;
		}

		public static KeyValueStoreBuilder AddGrpcClientKeyValueStore(this IServiceCollection collection,
			string name,
			ChannelBase grpcChannel)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.RegisterKeyValueStoreRequestHandler<GrpcClientKeyValueStoreRequestHandler>();
			builder.Services.TryAddSingleton(sp => ActivatorUtilities.CreateInstance<GrpcClientKeyValueStoreRequestHandler>(sp, grpcChannel));
			return builder;
		}

		public static IServiceCollection AddGrpcServerKeyValueStore(this IServiceCollection collection, string keyValueStoreName)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			
			collection.AddSingleton(sp =>
			{
				IKeyValueStore keyValueStore = sp.GetRequiredService<IServiceManager<IKeyValueStore>>().Get(keyValueStoreName);
				return ActivatorUtilities.CreateInstance<GrpcServerKeyValueStore>(sp, keyValueStore);
			});
			
			return collection;
		}
	}
}