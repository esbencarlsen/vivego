using System;
using System.Globalization;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using vivego.Serializer.NewtonJsonSerializer;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.Serializer
{
	public static class SerializerConfigurationExtensions
	{
		public static IServiceBuilder AddNewtonSoftJsonSerializer(this IServiceCollection collection,
			string? name = default,
			Action<JsonSerializerSettings>? configure = null)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));

			IServiceBuilder builder = new SerializerServiceBuilder<NewtonJsonSerializerRequestHandler>(name ?? nameof(NewtonJsonSerializerRequestHandler), collection);

			JsonSerializerSettings options = new()
			{
				Culture = CultureInfo.InvariantCulture,
				DateParseHandling = DateParseHandling.DateTimeOffset,
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				DateTimeZoneHandling = DateTimeZoneHandling.Utc,
				Formatting = Formatting.None,
				TypeNameHandling = TypeNameHandling.None,
				TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
			};
			configure?.Invoke(options);
			builder.Services.AddSingleton(options);
			builder.Services.AddSingleton<NewtonJsonSerializerRequestHandler>();

			return builder;
		}
	}
}