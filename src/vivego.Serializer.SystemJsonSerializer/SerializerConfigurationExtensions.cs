using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using vivego.core;
using vivego.Serializer.SystemJsonSerializer;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.Serializer
{
	public static class SerializerConfigurationExtensions
	{
		public static IServiceBuilder AddSystemJsonSerializer(this IServiceCollection collection,
			string? name = default,
			Action<JsonSerializerOptions>? configure = null)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			IServiceBuilder builder = new SerializerServiceBuilder<SystemJsonSerializerRequestHandler>(name ?? nameof(SystemJsonSerializerRequestHandler), collection);

			JsonSerializerOptions jsonSerializerOptions = new()
			{
				WriteIndented = false,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				IgnoreReadOnlyProperties = true,
				AllowTrailingCommas = true,
				PropertyNameCaseInsensitive = false
			};
			configure?.Invoke(jsonSerializerOptions);
			builder.Services.AddSingleton(jsonSerializerOptions);
			builder.Services.AddSingleton<SystemJsonSerializerRequestHandler>();
			builder.Services.AddSingleton<TypeNameHelper>();

			return builder;
		}
	}
}
