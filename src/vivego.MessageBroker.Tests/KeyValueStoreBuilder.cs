using System;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue;

namespace vivego.MessageBroker.Tests
{
	public static class KeyValueStoreBuilder
	{
		public static IKeyValueStore MakeKeyValueStore(Action<IServiceCollection> setup)
		{
			if (setup is null) throw new ArgumentNullException(nameof(setup));
			IServiceCollection collection = new ServiceCollection();
			setup.Invoke(collection);
			IServiceProvider serviceProvider = collection.BuildServiceProvider(true);
			return serviceProvider.GetRequiredService<IKeyValueStore>();
		}
	}
}
