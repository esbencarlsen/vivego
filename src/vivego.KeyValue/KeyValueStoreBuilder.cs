using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;
using vivego.MediatR;
using vivego.ServiceBuilder;

namespace vivego.KeyValue;

public sealed class KeyValueStoreBuilder : DefaultServiceBuilder<IKeyValueStore>
{
	public KeyValueStoreBuilder(
		string? name,
		IServiceCollection serviceCollection) : base(name ?? "Default", serviceCollection)
	{
		Services.AddSingleton<IKeyValueStore>(sp => ActivatorUtilities.CreateInstance<DefaultKeyValueStore>(sp, Name));

		// Exception Handlers
		this.AddExceptionLoggingPipelineBehaviour<SetRequest>(request => $"Error while processing SetRequest for key: {request.Entry.Key}");
		this.AddExceptionLoggingPipelineBehaviour<GetRequest>(request => $"Error while processing GetRequest for key: {request.Key}");
		this.AddExceptionLoggingPipelineBehaviour<DeleteRequest>(request => $"Error while processing DeleteRequest for key: {request.Entry.Key}");
		this.AddExceptionLoggingPipelineBehaviour<FeaturesRequest>(_ => "Error while processing FeatureRequest");
	}

	public void RegisterKeyValueStoreRequestHandler<T>() where T : IKeyValueStoreRequestHandler
	{
		Services.AddSingleton<IRequestHandler<SetRequest, string>>(sp => sp.GetRequiredService<T>());
		Services.AddSingleton<IRequestHandler<GetRequest, KeyValueEntry>>(sp => sp.GetRequiredService<T>());
		Services.AddSingleton<IRequestHandler<DeleteRequest, bool>>(sp => sp.GetRequiredService<T>());
		Services.AddSingleton<IRequestHandler<FeaturesRequest, KeyValueStoreFeatures>>(sp => sp.GetRequiredService<T>());
		Services.AddSingleton<IRequestHandler<ClearRequest, Unit>>(sp => sp.GetRequiredService<T>());
	}
}
