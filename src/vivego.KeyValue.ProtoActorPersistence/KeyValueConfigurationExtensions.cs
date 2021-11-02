using System;

using Microsoft.Extensions.DependencyInjection;

using Proto.Persistence;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.KeyValue.ProtoActorPersistence
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceCollection AddProtoActorKeyValueStoreProvider(this IServiceCollection collection,
			string? name = default,
			TimeSpan timeToLive = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			name ??= "Default";
			collection.AddOptions<KeyValueStoreProtoActorProviderOptions>(name).Configure(options => options.TimeToLive = timeToLive);
			collection.AddSingleton<IProvider>(sp =>
			{
				IKeyValueStore keyValueStore = sp
					.GetRequiredService<IServiceManager<IKeyValueStore>>()
					.Get(name);
				return ActivatorUtilities.CreateInstance<KeyValueStoreProtoActorProvider>(sp, keyValueStore);
			});

			return collection;
		}
	}
}
