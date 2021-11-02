using System;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Http;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class  KeyValueConfigurationExtensions
	{
		public static IMvcBuilder AddHttpKeyValueServerController(this IMvcBuilder mvcBuilder)
		{
			if (mvcBuilder is null) throw new ArgumentNullException(nameof(mvcBuilder));
			mvcBuilder
				.AddApplicationPart(typeof(HttpServerControllerKeyValueStore).Assembly)
				.AddControllersAsServices();
			return mvcBuilder;
		}

		public static IServiceBuilder AddHttpClientKeyValueStore(this IServiceCollection collection,
			string name,
			Uri serverBaseAddress,
			Action<IHttpClientBuilder>? configure = default)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (serverBaseAddress is null) throw new ArgumentNullException(nameof(serverBaseAddress));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.RegisterKeyValueStoreRequestHandler<HttpClientKeyValueStoreRequestHandler>();
			IHttpClientBuilder httpClientBuilder = builder.Services
				.AddHttpClient(nameof(HttpClientKeyValueStoreRequestHandler))
				.ConfigureHttpClient(httpClient => httpClient.BaseAddress = serverBaseAddress);
			configure?.Invoke(httpClientBuilder);
			builder.Services.AddSingleton<HttpClientKeyValueStoreRequestHandler>();
			return builder;
		}
	}
}
