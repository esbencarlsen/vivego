using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.MediatR;
using vivego.Serializer.Model;
using vivego.ServiceBuilder;

namespace vivego.Serializer
{
	public sealed class SerializerServiceBuilder<T> : DefaultServiceBuilder<ISerializer>
		where T : class, IRequestHandler<SerializeValueRequest, SerializedValue>, IRequestHandler<DeSerializeValueRequest, object?>
	{
		public SerializerServiceBuilder(string name, IServiceCollection serviceCollection) : base(name, serviceCollection)
		{
			Services.AddSingleton<ISerializer>(sp => ActivatorUtilities.CreateInstance<DefaultSerializer>(sp, name));

			Services.AddSingleton<IRequestHandler<SerializeValueRequest, SerializedValue>, T>(sp => sp.GetRequiredService<T>());
			Services.AddSingleton<IRequestHandler<DeSerializeValueRequest, object?>, T>(sp => sp.GetRequiredService<T>());

			// Exception Handlers
			Services.AddExceptionLoggingPipelineBehaviour<SerializeValueRequest>(_ => $"Error while processing {nameof(SerializeValueRequest)}");
			Services.AddExceptionLoggingPipelineBehaviour<DeSerializeValueRequest>(_ => $"Error while processing {nameof(DeSerializeValueRequest)}");
		}
	}
}
