using Microsoft.Extensions.DependencyInjection;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.Client.Http;

public static class ConfigurationExtensions
{
	public static IServiceCollection AddHttpMessageBroker(this IServiceCollection serviceCollection)
	{
		if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

		serviceCollection.AddSingleton<IMessageBroker, HttpMessageBroker>();
		serviceCollection
			.AddHttpClient(nameof(HttpMessageBroker))
			.ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"));

		return serviceCollection;
	}
}
