using System;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue;

namespace vivego.Orleans.KeyValueProvider.Tests
{
	public static class KeyValueStoreBuilder
	{
		public static IKeyValueStore MakeKeyValueStore(Action<IServiceCollection> setup)
		{
			IServiceCollection collection = new ServiceCollection();
			setup?.Invoke(collection);
			IServiceProvider serviceProvider = collection.BuildServiceProvider(true);
			return serviceProvider.GetRequiredService<IKeyValueStore>();
		}
	}
}