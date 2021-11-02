using System;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.InMemory;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddInMemoryKeyValueStore(this IServiceCollection collection,
			string? name = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.RegisterKeyValueStoreRequestHandler<InMemoryKeyValueStoreRequestHandler>();
			builder.Services.AddSingleton<InMemoryKeyValueStoreRequestHandler>();
			builder.DependsOn<IMemoryCache>();

			return builder;
		}
	}
}
