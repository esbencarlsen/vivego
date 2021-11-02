using Microsoft.Extensions.DependencyInjection;

using vivego.Serializer;
using vivego.ServiceBuilder;

namespace vivego.KeyValue;

public sealed class TypedKeyValueStoreBuilder : DefaultServiceBuilder<ITypedKeyValueStore>
{
	public TypedKeyValueStoreBuilder(string name, IServiceCollection serviceCollection) : base(name, serviceCollection)
	{
		DependsOn<ISerializer>();
		Services.AddSingleton<ITypedKeyValueStore>(sp => ActivatorUtilities.CreateInstance<TypedDefaultKeyValueStore>(sp, Name));
	}
}
