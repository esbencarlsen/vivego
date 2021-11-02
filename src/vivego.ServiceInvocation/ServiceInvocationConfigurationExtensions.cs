using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.ServiceInvocation
{
	public static class ServiceInvocationConfigurationExtensions
	{
		public static IServiceBuilder AddServiceInvocation(this IServiceCollection collection,
			string? providerName = default,
			Action<DefaultServiceInvocationOptions>? configure = default,
			Action<IHttpClientBuilder>? configureHttpClient = default,
			IAsyncPolicy<ServiceInvocationEntryResponse>? policy = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			IServiceBuilder serviceBuilder = new ServiceInvocationServiceBuilder(providerName ?? "Default",
				collection,
				configure,
				configureHttpClient,
				policy);
			return serviceBuilder;
		}
	}
}